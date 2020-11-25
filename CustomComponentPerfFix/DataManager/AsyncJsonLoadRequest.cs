using HBS.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueTechPerfFixes.DataManager
{
    public static class AsyncJsonLoadRequest
    {
		private static readonly ILog logger = Logger.GetLogger("DataLoader", LogLevel.Log);

		public static async Task LoadResource(string path, Action<string> handler, bool monitor)
		{
			try
			{
				using (FileStream arg = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					StreamReader sr = new StreamReader(arg);
					await sr.ReadToEndAsync();

					// TODO: Add DataLoader.Entry refernces here, so file update monitoring can happen -or- replicate with our own
					handler(sr.ToString());
				}
			}
			catch (Exception exception)
			{
				string message = $"LoadResource() - Caught exception while loading [{path}]";
				logger.LogError(message);
				logger.LogException(exception);
				handler(null);
			}
		}
	}
}
