using System;
using System.Collections.Generic;

namespace JSLCore.Event
{
    public class EventManager : Singleton<EventManager>
	{
        private class ListenerContainer : IComparable<ListenerContainer>
		{
			public IEventListener Listener { get; private set; }
			public int Priority { get; private set; }

			public ListenerContainer(IEventListener listener, int priority)
			{
				Listener = listener;
				Priority = priority;
			}

            public int CompareTo(ListenerContainer listener)
            {
                if (null == listener)
                {
                    return 0;
                }
                else
                {
                    return listener.Priority.CompareTo(this.Priority);
                }
            }
		}

        private Dictionary<int, List<ListenerContainer>> m_eventListenerContainers;
        private readonly Dictionary<int, ListenerContainer[]> m_eventListenerArrays;

        public EventManager()
		{
            m_eventListenerContainers = new Dictionary<int, List<ListenerContainer>>();
            m_eventListenerArrays = new Dictionary<int, ListenerContainer[]>();
		}

        public void RegisterListener(int eventId, IEventListener listener, int priority = 0)
		{
			if(!m_eventListenerContainers.ContainsKey(eventId))
			{
				m_eventListenerContainers[eventId] = new List<ListenerContainer>();
			}

			List<ListenerContainer> listeners = m_eventListenerContainers[eventId];
            int listenerCount = listeners.Count;
            for (int i = 0; i < listenerCount; i++)
            {
                if (listeners[i].Listener == listener)
                {
                    JSLDebug.LogException(new Exception("[EventManager] - Listener is already registered for this object."));
                    return;
                }
            }

			listeners.Add(new ListenerContainer(listener, priority));
            listeners.Sort();

            m_eventListenerContainers[eventId] = listeners;

            if (m_eventListenerArrays.ContainsKey(eventId))
            {
                m_eventListenerArrays[eventId] = listeners.ToArray();
            }
            else
            {
                m_eventListenerArrays.Add(eventId, listeners.ToArray());
            }
		}

        public void RemoveListener(int eventId, IEventListener listener)
		{
			if(m_eventListenerContainers.ContainsKey(eventId))
			{
				ListenerContainer tempListener;
                int listenerCount = m_eventListenerContainers[eventId].Count;

                List<ListenerContainer> removeListeners = new List<ListenerContainer>();

                for(int i = 0; i < listenerCount; i++)
				{
                    tempListener = m_eventListenerContainers[eventId][i];

					if(tempListener.Listener == listener)
					{
                        removeListeners.Add(tempListener);
					}
				}

                listenerCount = removeListeners.Count;
                for (int i = 0; i < listenerCount; i++)
                {
                    m_eventListenerContainers[eventId].Remove(removeListeners[i]);
                }

                if (m_eventListenerArrays.ContainsKey(eventId))
                {
                    m_eventListenerArrays[eventId] = m_eventListenerContainers[eventId].ToArray();
                }
                else
                {
                    m_eventListenerArrays.Add(eventId, m_eventListenerContainers[eventId].ToArray());
                }
			}
		}
        
        public EventResult SendEvent(int eventId, object eventData = null)
		{
            ListenerContainer[] containers;
            if (m_eventListenerArrays.TryGetValue(eventId , out containers))
			{
				EventResult result;
                int listenerCount = containers.Length;

                for(int i = 0; i < listenerCount; i++)
				{
                    if (containers[i] == null) continue;

                    result = containers[i].Listener.OnEvent(eventId, eventData);

                    if (null == result)
                    {
                        continue;
                    }

                    return result;
				}
			}

			return null;
		}
	}
}