using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace ErpMobile.Services;

/// <summary>
/// تغذية راجعة أثناء المسح: صوت "بيب" عند قراءة الباركود + اهتزاز اختياري.
/// </summary>
public static class ScanFeedback
{
    private const string BeepFile = "beep.wav";
    private static bool _playing;

    /// <summary>تشغيل صوت البيب عند قراءة باركود ناجح.</summary>
    public static void Beep()
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(PlayAndroidBeep);
#elif WINDOWS
        _ = Task.Run(() =>
        {
            try { Console.Beep(); } catch (Exception) { /* تجاهل */ }
        });
#else
        // iOS وأخرى: نكتفي بالاهتزاز (يُستدعى مع Beep من Success())
#endif
    }

#if ANDROID
    /// <summary>تشغيل beep.wav من ملفات التطبيق على أندرويد.</summary>
    private static void PlayAndroidBeep()
    {
        if (_playing)
        {
            return; // منع تراكب الأصوات عند المسح السريع
        }

        _playing = true;
        Android.Media.MediaPlayer? player = null;
        try
        {
            var context = Android.App.Application.Context;
            var uri = Android.Net.Uri.Parse($"file:///android_asset/{BeepFile}");
            player = Android.Media.MediaPlayer.Create(context, uri);

            if (player is not null)
            {
                player.Completion += (_, _) =>
                {
                    _playing = false;
                    try { player.Release(); } catch (Exception) { /* تجاهل */ }
                };
                player.Start();

                // شبكة أمان: نضمن تحرير العلامة لو لم يصل حدث الاكتمال
                _ = Task.Delay(1500).ContinueWith(_ => _playing = false);
            }
            else
            {
                _playing = false;
            }
        }
        catch (Exception)
        {
            _playing = false;
            player?.Release();
        }
    }
#endif

    /// <summary>اهتزاز خفيف (اختياري) عند القراءة.</summary>
    public static void Vibrate()
    {
        try
        {
            if (Vibration.Default.IsSupported)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(70));
            }
        }
        catch (Exception)
        {
            // تجاهل
        }
    }

    /// <summary>صوت + اهتزاز معاً عند إضافة صنف.</summary>
    public static void Success()
    {
        Beep();
        Vibrate();
    }
}
