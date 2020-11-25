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

		public static async Task LoadResource(string path, Action<string> handler)
		{
			try
			{
				using (FileStream arg = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					StreamReader sr = new StreamReader(arg);
					//logger.Log($"Reading file at path: {path}");
					string content = await sr.ReadToEndAsync();

					// TODO: Add DataLoader.Entry references here, so file update monitoring can happen -or- replicate with our own
					//logger.Log($"Handling file at path: {path} with content: {content}");
					handler(content);
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
