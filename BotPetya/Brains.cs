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
		#region Constants

		private const string BlankMessage = "BlankMessage";

		private const string CommandAdded = "Слово силы изучено!";

		private const string UnfoundCommand = "Словарный запас кончился!";

		private const string ErrorAddingCommand = "Слово силы слишком тяжелое, обратись к создателю!!!!";

		#endregion

		#region Data

		private BaseCommands _baseCommands = new BaseCommands();

		#endregion

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

		public Tuple<long?, Command> ProcessingEventMessages(VkApi vk, long code, List<object> item)
		{
			long? currentUserId = null;
			Command command = null;
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
					currentUserId = (long)item[3];
					var currentMessage = (string)item[5];

					var findCommand = _baseCommands.LoadCommand(new Command { Value = currentMessage.ToLower() });
					if(findCommand != null)
					{
						command = findCommand;
					}
					else
					{
						if(currentMessage.Count(x => x.Equals('~')) > 2)
						{
							var newCommand = CreateCommand(currentMessage);
							if(newCommand != null)
							{
								command = new Command
								{
									Value = CommandAdded,
									Answers = new List<Answer>
									{
										new Answer
										{
											Value = CommandAdded
										}
									}
								};
							}
							else command = new Command { Value = ErrorAddingCommand };
						}
						else
						{
							command = new Command { Value = BlankMessage };
						}
					}
					break;
			}

			return Tuple.Create(currentUserId, command);
		}

		public bool SendingResponse(VkApi vk, long userId, Command comand)
		{
			long isSend = -1;
			if(!comand.Value.Equals(BlankMessage))
			{
				isSend = vk.Messages.Send(new MessagesSendParams
				{
					UserId = userId,
					Message = comand.RandomAnswer().Value,
				});
			}
			else
			{
				isSend = vk.Messages.Send(new MessagesSendParams
				{
					UserId = userId,
					Message = UnfoundCommand,
				});
			}
			return isSend == -1 ? false : true;
		}

		private Command CreateCommand(string message)
		{
			var commands = _baseCommands.LoadAllCommands();
			var newCommand = message.Substring(1, message.Length - 2).Split('~');
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
				Command result;
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
					result = _baseCommands.AddComand(new Command
					{
						Value = newCommand[0].ToLower(),
						Answers = anserList.Count > 0 ? anserList : null,
					});
				}
				if(result != null)
				{
					return result;
				}
			}

			return null;
		}
	}
}
