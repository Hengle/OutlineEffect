using System.Collections.Generic;

namespace JSLCore.Event
{
	public class EventListener : IEventListener, IDestroy
	{
        public delegate EventResult EventCallback(object eventData);

		protected class EventListenerData
		{
			public EventCallback Callback;
			public bool CallWhenInactive;
		}

        protected Dictionary<int, EventListenerData> m_eventListeners;

		private bool m_active;
		public bool Active
		{
			get { return m_active; }
			set
			{
				m_active = value;
				OnActiveChanged();
			}
		}

		protected virtual void OnActiveChanged()
		{

		}

		public EventListener()
		{
            Active = true;
            m_eventListeners = new Dictionary<int, EventListenerData>();
		}

        public void ListenForEvent(int eventName, EventCallback callback, bool callWhenInactive = false, int priority = 0)
		{
            EventListenerData eventListenerData = new EventListenerData
            {
                Callback = callback,
                CallWhenInactive = callWhenInactive
            };

            m_eventListeners[eventName] = eventListenerData;

			EventManager.Instance.RegisterListener(eventName, this, priority);
		}

        public void StopListenForEvent(int eventName)
		{
			if(m_eventListeners.ContainsKey(eventName))
			{
				m_eventListeners.Remove(eventName);

                EventManager.Instance.RemoveListener(eventName, this);
			}
		}

		#region IEventListener
        public EventResult OnEvent(int eventName, object eventData)
		{
			if(m_eventListeners.ContainsKey(eventName))
			{
				EventListenerData eventListenerData = m_eventListeners[eventName];
				
				if(!Active && !eventListenerData.CallWhenInactive)
				{
					return null;
				}
				
				if(eventListenerData.Callback != null)
				{
					return eventListenerData.Callback(eventData);
				}
			}
			
			return null;
		}
		#endregion

		#region IDestroyable
		public virtual void Destroy()
		{
            foreach(int eventName in m_eventListeners.Keys)
			{
                EventManager.Instance.RemoveListener(eventName, this);
			}

			m_eventListeners.Clear();
		}
		#endregion
	}
}