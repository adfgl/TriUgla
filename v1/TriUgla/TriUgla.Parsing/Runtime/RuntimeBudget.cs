using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TriUgla.Parsing.Runtime
{
    public sealed class RuntimeBudget
    {
        public enum EStopReason
        {
            None,
            StepLimit,
            Timeout,
            Cancellation,
            FullStop
        }

        // Check wall-clock every N ticks (cheap bitmask throttle)
        const int TimeCheckMask = 0x3FF; // 1024
        const long NoDeadline = long.MaxValue;
        static readonly TimeSpan Infinite = Timeout.InfiniteTimeSpan;

        // Timestamp math (Stopwatch ticks, not TimeSpan ticks)
        static readonly double TimeSpanTicksPerSwTick =
            (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        long _stepsRemaining = long.MaxValue; // "unlimited" sentinel
        int _throttleCounter;
        long _deadlineSwTicks = NoDeadline;

        public TimeSpan TimeLimit { get; set; } = Infinite;
        public CancellationToken Token { get; set; }

        public bool Aborted { get; set; }
        public EStopReason Reason { get; set; } = EStopReason.None;

        /// <summary>
        /// Sets a wall-clock limit from "now". Non-positive disables the time limit.
        /// </summary>
        public void SetTimeLimit(TimeSpan limit)
        {
            if (limit <= TimeSpan.Zero)
            {
                DisableTimeLimit();
                return;
            }

            TimeLimit = limit;
            var now = Stopwatch.GetTimestamp();
            _deadlineSwTicks = now + (long)(limit.Ticks / TimeSpanTicksPerSwTick);

            Aborted = false;
            Reason = EStopReason.None;
            _throttleCounter = 0;
        }

        /// <summary>
        /// Sets step budget. steps &lt;= 0 means immediate exhaustion (next Tick() fails).
        /// </summary>
        public void SetStepBudget(long steps)
        {
            _stepsRemaining = steps <= 0 ? 0 : steps;
            Aborted = false;
            Reason = EStopReason.None;
        }

        /// <summary>
        /// Manually stop all execution.
        /// </summary>
        public void FullStop()
        {
            Aborted = true;
            Reason = EStopReason.FullStop;
        }

        /// <summary>
        /// Resets all limits and state.
        /// </summary>
        public void Reset()
        {
            Aborted = false;
            Reason = EStopReason.None;
            _stepsRemaining = long.MaxValue;
            _throttleCounter = 0;
            Token = default;
            DisableTimeLimit();
        }

        /// <summary>
        /// Cheap per-iteration check. Cancels are immediate; time is throttled.
        /// Returns false when the budget is exceeded or canceled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Tick()
        {
            if (Aborted) return false;

            // Step budget first
            if (_stepsRemaining != long.MaxValue && --_stepsRemaining <= 0)
            {
                Aborted = true;
                Reason = EStopReason.StepLimit;
                return false;
            }

            // Cancellation every tick (cheap & responsive)
            if (Token.IsCancellationRequested)
            {
                Aborted = true;
                Reason = EStopReason.Cancellation;
                return false;
            }

            // Time check throttled
            if ((++_throttleCounter & TimeCheckMask) == 0 && IsTimeLimitReached())
            {
                Aborted = true;
                Reason = EStopReason.Timeout;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Immediate full check (use around long operations).
        /// Returns false when the budget is exceeded or canceled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckNow()
        {
            if (Aborted) return false;

            if (_stepsRemaining != long.MaxValue && _stepsRemaining <= 0)
            {
                Aborted = true;
                Reason = EStopReason.StepLimit;
                return false;
            }

            if (Token.IsCancellationRequested)
            {
                Aborted = true;
                Reason = EStopReason.Cancellation;
                return false;
            }

            if (IsTimeLimitReached())
            {
                Aborted = true;
                Reason = EStopReason.Timeout;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the reason why the budget was aborted (Timeout, StepLimit, Cancellation, FullStop, or None).
        /// </summary>
        public EStopReason GetStopReason() => Reason;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DisableTimeLimit()
        {
            TimeLimit = Infinite;
            _deadlineSwTicks = NoDeadline;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsTimeLimitReached()
        {
            if (_deadlineSwTicks == NoDeadline) return false;
            return Stopwatch.GetTimestamp() >= _deadlineSwTicks;
        }
    }
}
