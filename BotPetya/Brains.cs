using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace BotPetya
{
	public class Brains
	{
		//6318801
		//2823d6705766638647e5914013e17652e11bc82508df1084681bb6a7abfc66cfb243575f9faeec8b1ba77

		private Random _random = new Random();
		private BaseCommands _baseCommands = new BaseCommands();

		public void Initialization(VkApi vk, ulong appId, string token)
		{
			vk.Authorize(new ApiAuthParams
			{
				ApplicationId = appId,
				AccessToken = token,
				TokenExpireTime = 0,

			});
		}

		public Message EventActivationBot(VkApi vk, LongPollServerResponse server)
		{
			WebRequest request = WebRequest.Create($"https://{server.Server}?act=a_check&key={server.Key}&ts={server.Ts}&wait=25&mode=2&version=2");
			using(var response = request.GetResponse())
			using(var stream = response.GetResponseStream())
			using(var stramReader = new StreamReader(stream))
			{
				return JsonConvert.DeserializeObject<Message>(stramReader.ReadLine());
			}
		}

		public LongPollHistoryResponse GetHistoryMessages(VkApi vk, LongPollServerResponse server)
		{
			return vk.Messages.GetLongPollHistory(new MessagesGetLongPollHistoryParams
			{
				Fields = new UsersFields(),
				Pts = server.Pts,
				Ts = server.Ts,
			});
		}

		public Tuple<long?, string> ProcessingEventMessages(VkApi vk, long code, List<object> item)
		{
			long? currentUserId = null;
			var answer = string.Empty;
			switch(code)
			{
				case 7:
					var messageId = new List<long>();
					messageId.Add((long)item[2]);
					currentUserId = (long)item[1];
					vk.Messages.MarkAsRead(messageId, currentUserId.ToString());
					break;
				case 12:
					var dialogs = vk.Messages.GetDialogs(new MessagesDialogsGetParams
					{
						Unanswered=true,
					});
					var userId = (long)item[1];
				 	var dialog = dialogs.Messages.FirstOrDefault(x => x.UserId == userId);
					if(dialog != null)
					{
						dialog.ReadState = MessageReadState.Readed;
					}
					break;
				case 4:
					var commands = _baseCommands.LoadAllComands();
					currentUserId = (long)item[3];
					var currentMessage = (string)item[5];
					var findCommand = commands.FirstOrDefault(x => x.Value.Equals(currentMessage));
					if(findCommand != null)
					{
						answer = findCommand.Answers[_random.Next(0, findCommand.Answers.Count)].Value;
					}
					else
					{
						if(currentMessage.Count(x => x.Equals('~')) > 2)
						{
							var newCommand = currentMessage.Substring(1, currentMessage.Length - 2).Split('~');
							if(newCommand.Length > 0)
							{
								var answers = newCommand[1].Split('|');
								var anserList = new List<Answer>();
								if(answers.Length > 0)
								{
									foreach(var newAnswer in answers)
									{
										if(string.IsNullOrWhiteSpace(newAnswer)) continue;
										anserList.Add(new Answer
										{
											Value = newAnswer,
										});
									}
								}

								var commandExist = commands.FirstOrDefault(x => x.Value.Equals(newCommand[0]));
								bool result;
								if(commandExist != null)
								{
									foreach(var value in anserList)
									{
										commandExist.Answers.Add(value);
									}

									result = _baseCommands.AddComand(commandExist);
								}
								else
								{
									result = _baseCommands.AddComand(new Comand
									{
										Value = newCommand[0],
										Answers = anserList.Count > 0 ? anserList : null,
									});
								}
								if(result)
								{
									answer = "Команда добавлена";
								}
							}
						}
						else
						{
							answer = "error send";
						}
					}
					break;
			}

			return Tuple.Create(currentUserId, answer);
		}

		public bool SendingResponse(VkApi vk, long userId, string answer)
		{
			long isSend = -1;
			if(!answer.Equals("error send"))
			{
				isSend = vk.Messages.Send(new MessagesSendParams
				{
					UserId = userId,
					Message = answer,
				});
			}
			else
			{
				isSend = vk.Messages.Send(new MessagesSendParams
				{
					UserId = userId,
					Message = "Братка я тебя не понимаю, но ты можешь поднять мой IQ",
				});
			}
			return isSend == -1 ? false : true;
		}
	}
}
