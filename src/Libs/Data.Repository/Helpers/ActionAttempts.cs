using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Helpers
{
    public class ActionAttempts
    {
        public int CurrentAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public int AttemptsRemaining { get { return MaxAttempts - CurrentAttempts; } }
        public Func<bool> Check { get; set; }
        public Func<bool> TimeCheck { get; set; }
        public Action IncrementAttempts { get; set; }
        public Action DecrementAttempts { get; set; }
        public bool IsClear => CurrentAttempts == 0;
        public bool IsDurty => !IsClear;
        public bool IsExhausted => !IsAvailable;
        public bool IsAvailable => CurrentAttempts < MaxAttempts;

        public void Increment()

        {
            CurrentAttempts++;
            IncrementAttempts?.Invoke();
        }
    }
}

