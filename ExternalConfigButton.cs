using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;

public class ExternalConfigButton : MonoBehaviour
{
    public Text RemoteConfigContents;
    Firebase.FirebaseApp app;
    Firebase.RemoteConfig.FirebaseRemoteConfig remote;
    Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
    protected bool isFirebaseInitialized = false;
    // Start is called before the first frame update
    void Start()
    {
        RemoteConfigContents = RemoteConfigContents.GetComponent<Text>();
        RemoteConfigContents.text = null;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void onClick()
    {
        this.RemoteConfigContents.text = null;
        this.RemoteConfigContents.text = "External" + System.Environment.NewLine;
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                try
                {
                    InitializeFirebase();
                    FetchDataAsync();
                    DisplayAllKeys();
                    DisplayData();
                }
                catch (Exception e)
                {
                    DebugLog(e.ToString());
                }
            }
            else
            {
                DebugLog(
                  "Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }
    // Initialize remote config, and set the default values.
    void InitializeFirebase()
    {
        app = Firebase.FirebaseApp.Create(new Firebase.AppOptions()
        {
            ProjectId = "<project-id>",
            ApiKey = "<api-key>",
            AppId = "<app-id>"
        }, "External");
        remote = Firebase.RemoteConfig.FirebaseRemoteConfig.GetInstance(app);

        DebugLog("created instance projectid=" + app.Options.ProjectId);
        DebugLog("created instance appid=" + app.Options.AppId);
        DebugLog("created instance apikey=" + app.Options.ApiKey);
        // [START set_defaults]
        System.Collections.Generic.Dictionary<string, object> defaults =
          new System.Collections.Generic.Dictionary<string, object>();

        // These are the values that are used if we haven't fetched data from the
        // server
        // yet, or if we ask for values that the server doesn't have:
        defaults.Add("ExampleKey", "default local string");
        remote.SetDefaultsAsync(defaults)
          .ContinueWithOnMainThread(task => {
              // [END set_defaults]
              DebugLog("RemoteConfig configured and ready!");
              isFirebaseInitialized = true;
          });
    }

    // Display the currently loaded data.  If fetch has been called, this will be
    // the data fetched from the server.  Otherwise, it will be the defaults.
    // Note:  Firebase will cache this between sessions, so even if you haven't
    // called fetch yet, if it was called on a previous run of the program, you
    //  will still have data from the last time it was run.
    public void DisplayData()
    {
        DebugLog("Current Data:");
        DebugLog("ExampleKey: " +
                 remote
                 .GetValue("ExampleKey").StringValue);
    }

    public void DisplayAllKeys()
    {
        DebugLog("Current Keys:");
        System.Collections.Generic.IEnumerable<string> keys =
            remote.Keys;
        foreach (string key in keys)
        {
            DebugLog("key=" + key);
        }
    }

    // [START fetch_async]
    // Start a fetch request.
    // FetchAsync only fetches new data if the current data is older than the provided
    // timespan.  Otherwise it assumes the data is "recent enough", and does nothing.
    // By default the timespan is 12 hours, and for production apps, this is a good
    // number. For this example though, it's set to a timespan of zero, so that
    // changes in the console will always show up immediately.
    public Task FetchDataAsync()
    {
        DebugLog("Fetching data...");
        System.Threading.Tasks.Task fetchTask =
        remote.FetchAsync(
            TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }
    //[END fetch_async]

    void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            DebugLog("Fetch canceled.");
        }
        else if (fetchTask.IsFaulted)
        {
            DebugLog("Fetch encountered an error.");
        }
        else if (fetchTask.IsCompleted)
        {
            DebugLog("Fetch completed successfully!");
        }

        var info = remote.Info;
        switch (info.LastFetchStatus)
        {
            case Firebase.RemoteConfig.LastFetchStatus.Success:
                remote.ActivateAsync()
                .ContinueWithOnMainThread(task => {
                    DebugLog(String.Format("Remote data loaded and ready (last fetch time {0}).",
                               info.FetchTime));
                });

                break;
            case Firebase.RemoteConfig.LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case Firebase.RemoteConfig.FetchFailureReason.Invalid:
                        DebugLog("Fetch failed invalid");
                        break;
                    case Firebase.RemoteConfig.FetchFailureReason.Error:
                        DebugLog("Fetch failed for unknown reason");
                        break;
                    case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                        DebugLog("Fetch throttled until " + info.ThrottledEndTime);
                        break;
                }
                break;
            case Firebase.RemoteConfig.LastFetchStatus.Pending:
                DebugLog("Latest Fetch call still pending.");
                break;
        }
    }

    // Output text to the debug log text field, as well as the console.
    public void DebugLog(string s)
    {
        RemoteConfigContents.text += s + System.Environment.NewLine;
    }
}
