using System;
using System.Collections.Generic;

public class TimerManager
{
    private class TimerInstance
    {
        public float TimeLeft;
        public Action<object> Callback;
        public object Cookie;
    }

    private List<TimerInstance> timers;
    private Stack<TimerInstance> timersToRemove;

    public TimerManager()
    {
        timers = new List<TimerInstance>();
        timersToRemove = new Stack<TimerInstance>();
        Service.UpdateManager.AddObserver(Update, true);
    }

    public void CreateTimer(float duration, Action<object> callback, object cookie)
    {
        TimerInstance timer = new TimerInstance {TimeLeft = duration, Callback = callback, Cookie = cookie };
        timers.Add(timer);
    }

    public void Update(float dt)
    {
        for (int i = 0, count = timers.Count; i < count; ++i)
        {
            TimerInstance currentTimer = timers[i];
            currentTimer.TimeLeft -= dt;
            if (timers[i].TimeLeft <= 0)
            {
                timers[i].Callback.Invoke(currentTimer.Cookie);
                timersToRemove.Push(timers[i]);
            }
        }

        while(timersToRemove.Count > 0)
        {
            timers.Remove(timersToRemove.Pop());
        }
    }
}
