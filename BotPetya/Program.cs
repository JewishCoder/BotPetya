using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;
using VkNet.Model;
using Newtonsoft.Json;

namespace BotPetya
{
	class Program
	{
		#region Data

		static VkApi _vk = new VkApi();

		static Message _message = new Message();

		static LongPollServerResponse _server;

		static Brains _brains = new Brains();

		static long? _currentUserId = null;

		#endregion

		static void RevivalPetya()
		{
			_brains.Initialization(_vk, 6318801, "2823d6705766638647e5914013e17652e11bc82508df1084681bb6a7abfc66cfb243575f9faeec8b1ba77");
			if(_vk.IsAuthorized)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("Авторизация прошла успешно");
				Console.ResetColor();

				var isEventMessage = true;
				while(true)
				{
					_server = _vk.Messages.GetLongPollServer(true);
					_message = _brains.EventActivationBot(_vk, _server);
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(_message == null ? "Ошибка активации" : "Активация бота");
					Console.ResetColor();

					_server.Ts = _message.Ts;
					if(_message.Updates.Count == 0)
					{

						Console.WriteLine("Ожидание сообщения");
						isEventMessage = false;
					}
					else
					{
						Console.WriteLine("Событие сообщения");
						isEventMessage = true;
					}

					if(isEventMessage)
					{
						var history = _brains.GetHistoryMessages(_vk, _server);
						_currentUserId = null;
						Command command = null;
						foreach(var update in _message.Updates)
						{
							var code = (long)update[0];
							var result = _brains.ProcessingEventMessages(_vk, code, update);
							_currentUserId = result.Item1;
							command=result.Item2;
							if(command != null) break;
						}

						if(_currentUserId.HasValue && command != null)
						{
							if(_brains.SendingResponse(_vk, _currentUserId.Value, command))
							{
								Console.ForegroundColor = ConsoleColor.Green;
								Console.WriteLine("Сообщение отправлено");
								Console.ResetColor();
								isEventMessage = true;
								_message.Updates.Clear();
							}
							else
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.WriteLine("При попытке отправить сообщение возникли трудности :С");
								Console.ResetColor();
								isEventMessage = false;
							}
						}
					}
				}
			}
		}

		static void Main(string[] args)
		{
			try
			{
				RevivalPetya();
			}
			catch(Exception exc)
			{
				Console.WriteLine("auth fail");
				Console.WriteLine(exc.Message, exc);
			}
			finally
			{
				RevivalPetya();
			}

			Console.ReadKey();
		}
	}
}
