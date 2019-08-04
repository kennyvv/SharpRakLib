using System.Collections.Generic;

namespace SharpRakLib.Server
{
	public class HookManager
	{
		public enum Hook
		{
			PacketRecieved,
			SessionOpened,
			SessionClosed
		}

		private readonly Dictionary<Hook, IHookRunnable> _hooks = new Dictionary<Hook, IHookRunnable>();

		public HookManager(RakNetServer server)
		{
			this.Server = server;
		}

		public RakNetServer Server { get; private set; }

		internal void ActivateHook(Hook hook, Session session, params object[] param)
		{
			lock (_hooks)
			{
				if (!_hooks.ContainsKey(hook)) return;
				_hooks[hook].OnHook(session, param);
			}
		}

		/**
		 * Adds a hook, which is a <code>HookRunnable</code> that is called when a
		 * specific event has occurred. One example of this is when a packet is received.
		 *
		 * @param hook The type of hook or event.
		 * @param r    The HookRunnable to be ran when the hook has been triggered.
		 * @see Hook
		 */

		public void AddHook(Hook hook, IHookRunnable r)
		{
			lock (_hooks)
			{
				_hooks.Add(hook, r);
			}
		}

		public interface IHookRunnable
		{
			void OnHook(Session session, params object[] param);
		}
	}
}