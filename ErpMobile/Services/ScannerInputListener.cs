using System.Diagnostics;

namespace ErpMobile.Services;

/// <summary>
/// يجعل حقل الباركود جاهزاً لاستقبال «ماسح الباركود USB» (وضع لوحة المفاتيح)
/// من غير أي نقر من المستخدم — مخصص للويندوز بس بيشتغل على كل المنصات.
///
/// الماسح عادةً بيبعت: الارقام بسرعة ثم Enter. الخدمة بتعالج الحالتين:
///   1) ضغط Enter  ──► يضيف الصنف فوراً (OnCompleted)
///   2) ماسح بدون Enter ──► يكتشف الكتابة بسرعة الماسح
///                         (متوسط أقل من 70 مللي ثانية لأكثر من 4 أحرف)
///                         وينتظر 300 مللي ثانية سكون ثم يضيف الصنف
///
/// الكتابة اليدوية العادية أبطأ من 70 مللي ثانية لكل حرف، فلا تُطلق الإضافة
/// التلقائية بالغلط.
/// </summary>
public sealed class ScannerInputListener : IDisposable
{
    /// <summary>أقل متوسط زمن بين حرفين يُعتبر سلوك ماسح (يدوياً أبطأ من كده).</summary>
    private const double ScannerIntervalMs = 70;

    /// <summary>عدد الحدث الأدنى قبل تأكيد أن الإدخال من ماسح.</summary>
    private const int MinChangesForBurst = 4;

    /// <summary>سكون (بالملي ثانية) بعد آخر حرف قبل الإضافة التلقائية.</summary>
    private const int IdleBeforeCommitMs = 300;

    /// <summary>أقصر باركود مقبول.</summary>
    private const int MinBarcodeLength = 6;

    private readonly Entry _entry;
    private readonly Func<string, Task> _onBarcode;
    private readonly List<double> _intervals = new();

    private long _lastChangeTicks;
    private bool _burstConfirmed;
    private CancellationTokenSource? _commitCts;
    private bool _disposed;

    public ScannerInputListener(Entry entry, Func<string, Task> onBarcode)
    {
        _entry = entry;
        _onBarcode = onBarcode;
        _entry.TextChanged += OnTextChanged;
        _entry.Completed += OnCompleted;
    }

    /// <summary>يوقف التقاط الإدخال مؤقتاً (أثناء عمل الماسح بالكاميرا مثلاً).</summary>
    public void Suspend()
    {
        CancelPending();
        _burstConfirmed = false;
        _intervals.Clear();
        _lastChangeTicks = 0;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        long now = Stopwatch.GetTimestamp();

        if (_lastChangeTicks != 0)
        {
            double elapsed = (now - _lastChangeTicks) * 1000.0 / Stopwatch.Frequency;

            if (elapsed > 250)
            {
                // انقطعت الكتابة — نبدأ سلسلة جديدة
                _intervals.Clear();
                _burstConfirmed = false;
                CancelPending();
            }
            else
            {
                _intervals.Add(elapsed);
            }
        }

        _lastChangeTicks = now;

        string text = _entry.Text?.Trim() ?? string.Empty;
        if (text.Length < MinBarcodeLength)
        {
            CancelPending();
            return;
        }

        if (!_burstConfirmed &&
            _intervals.Count >= MinChangesForBurst &&
            _intervals.Average() < ScannerIntervalMs)
        {
            _burstConfirmed = true;
        }

        if (_burstConfirmed)
        {
            ScheduleCommit();
        }
    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        // Enter = الماسح خلّص إرسال الباركود (أو المستخدم كتبه ودوس Enter)
        CancelPending();
        _ = CommitAsync();
    }

    private void ScheduleCommit()
    {
        CancelPending();

        var cts = new CancellationTokenSource();
        _commitCts = cts;
        _ = CommitAfterIdleAsync(cts.Token);
    }

    private async Task CommitAfterIdleAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(IdleBeforeCommitMs, token);
        }
        catch (TaskCanceledException)
        {
            return; // وصل حرف جديد قبل الإضافة
        }

        await CommitAsync();
    }

    private async Task CommitAsync()
    {
        string barcode = _entry.Text?.Trim() ?? string.Empty;
        if (barcode.Length < MinBarcodeLength)
        {
            return;
        }

        ResetState();

        // نفصل مؤقتاً حتى لا تعيد ضبط النص داخل المعالج إحداث التقاط جديد
        _entry.TextChanged -= OnTextChanged;
        try
        {
            await _onBarcode(barcode);
            _entry.Text = string.Empty;
        }
        catch (Exception)
        {
            // لا ندع خطأ المعالج يُسقط التطبيق
        }
        finally
        {
            if (!_disposed)
            {
                _entry.TextChanged += OnTextChanged;
            }

            ResetState();
        }
    }

    private void ResetState()
    {
        CancelPending();
        _burstConfirmed = false;
        _intervals.Clear();
        _lastChangeTicks = 0;
    }

    private void CancelPending()
    {
        _commitCts?.Cancel();
        _commitCts?.Dispose();
        _commitCts = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelPending();
        _entry.TextChanged -= OnTextChanged;
        _entry.Completed -= OnCompleted;
    }
}
