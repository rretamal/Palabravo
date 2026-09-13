using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.EventArgs;
#if ANDROID || IOS
using Plugin.Firebase.CloudMessaging;
#endif

namespace Palabravo.Services;

public sealed class EngagementNotificationService
{
    public const string UpdatesChannelId = "palabravo_updates";
    private const string ReminderPreference = "progress_reminders_enabled";
    private const string NewsPreference = "new_challenges_notifications_enabled";
    private const string TopicRegisteredPreference = "new_challenges_topic_registered";
    private const string NewsTopic = "new-challenges-es";
    private const int ProgressReminderId = 4101;
    private readonly INotificationService localNotifications;
    private bool cloudEventsAttached;

    public EngagementNotificationService(INotificationService localNotifications)
    {
        this.localNotifications = localNotifications;
        this.localNotifications.NotificationActionTapped += OnLocalNotificationTapped;
    }

    public bool ProgressRemindersEnabled => Preferences.Default.Get(ReminderPreference, false);
    public bool NewChallengesEnabled => Preferences.Default.Get(NewsPreference, false);

    public async Task<bool> SetProgressRemindersEnabledAsync(bool enabled)
    {
        if (enabled && !await EnsurePermissionAsync()) return false;
        Preferences.Default.Set(ReminderPreference, enabled);
        if (!enabled) localNotifications.Cancel(ProgressReminderId);
        return true;
    }

    public async Task<bool> SetNewChallengesEnabledAsync(bool enabled)
    {
        if (enabled && !await EnsurePermissionAsync()) return false;
        Preferences.Default.Set(NewsPreference, enabled);
        await SynchronizeTopicAsync();
        return true;
    }

    public async Task OnActivatedAsync()
    {
        localNotifications.Cancel(ProgressReminderId);
        AttachCloudMessagingEvents();
        await SynchronizeTopicAsync();
    }

    public async Task OnDeactivatedAsync()
    {
        if (!ProgressRemindersEnabled) return;
        localNotifications.Cancel(ProgressReminderId);
        await localNotifications.Show(new NotificationRequest
        {
            NotificationId = ProgressReminderId,
            Title = "Tu próximo reto te espera",
            Description = "Vuelve a Palabravo y sigue avanzando hacia el siguiente rango.",
            ReturningData = "palabravo://home",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTimeOffset.Now.AddDays(2),
                RepeatType = NotificationRepeat.No
            },
            Android = new Plugin.LocalNotification.Core.Models.AndroidOption.AndroidOptions
            {
                ChannelId = UpdatesChannelId,
                AutoCancel = true,
                LaunchAppWhenTapped = true
            }
        });
    }

    public async Task ResetAsync()
    {
        Preferences.Default.Set(ReminderPreference, false);
        Preferences.Default.Set(NewsPreference, false);
        localNotifications.Cancel(ProgressReminderId);
        await SynchronizeTopicAsync();
        Preferences.Default.Remove(ReminderPreference);
        Preferences.Default.Remove(NewsPreference);
    }

    private async Task<bool> EnsurePermissionAsync()
    {
        if (await localNotifications.AreNotificationsEnabled()) return true;
        return await localNotifications.RequestNotificationPermission();
    }

    private void AttachCloudMessagingEvents()
    {
#if ANDROID || IOS
        if (cloudEventsAttached) return;
        CrossFirebaseCloudMessaging.Current.NotificationTapped += (_, args) => Open(args.Notification.Data);
        cloudEventsAttached = true;
#endif
    }

    private async Task SynchronizeTopicAsync()
    {
#if ANDROID || IOS
        var shouldSubscribe = NewChallengesEnabled;
        var registered = Preferences.Default.Get(TopicRegisteredPreference, false);
        if (shouldSubscribe == registered) return;
        try
        {
            if (shouldSubscribe)
            {
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                await CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync(NewsTopic);
            }
            else
            {
                await CrossFirebaseCloudMessaging.Current.UnsubscribeFromTopicAsync(NewsTopic);
            }
            Preferences.Default.Set(TopicRegisteredPreference, shouldSubscribe);
        }
        catch
        {
            // Keep the desired preference and retry the topic synchronization next time the app opens.
        }
#else
        await Task.CompletedTask;
#endif
    }

    private static void OnLocalNotificationTapped(NotificationActionEventArgs args)
    {
        if (!string.Equals(args.Request.ReturningData, "palabravo://home", StringComparison.OrdinalIgnoreCase)) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current is not null) await Shell.Current.GoToAsync("//home");
        });
    }

    private static void Open(IDictionary<string, string>? data)
    {
        if (data is null || !data.TryGetValue("link", out var link)
            || !Uri.TryCreate(link, UriKind.Absolute, out var uri)) return;
        IPlatformApplication.Current?.Services.GetService<ReferralService>()?.Receive(uri);
    }
}
