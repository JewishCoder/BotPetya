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
using VkNet.Model.Attachments;
using VkNet.Utils;
using Newtonsoft.Json.Linq;

namespace BotPetya
{
	public class Brains
	{
		#region Constants

		private const string BlankMessage = "BlankMessage";

		private const string CommandAdded = "Слово силы изучено!";

		private const string UnfoundCommand = "Словарный запас кончился!";

		private const string ErrorAddingCommand = "Слово силы слишком тяжелое, обратись к создателю!!!!";

		private const string StickerMessage = "Отправь стикер и я его запомню!";

		private const string StickerCommandAdd = "стикер";

		#endregion

		#region Data

		private BaseCommands _baseCommands = new BaseCommands();

		private Random _random = new Random();

		private Command _currentStickerCommand;

		private bool _stickerCommandExist = false;

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
						if(currentMessage.Count(x => x.Equals('~')) > 2 || _currentStickerCommand != null)
						{
							var media = ParsingAttachmets(item[6]);
							var newCommand = CreateCommand(currentMessage, media);
							if(newCommand != null)
							{
								if(_currentStickerCommand != null && media != null)
								{
									_currentStickerCommand = null;
									_stickerCommandExist = false;
								}
								if(_currentStickerCommand == null)
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
								else
								{
									command = newCommand;
								}
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
				Sticker stickerId = null;
				if(comand.Attachments != null && comand.Answers == null)
				{
					stickerId = (Sticker)comand.RandomAttachment(AttachmentTypes.Sticker);
				}
				
				if(comand.Attachments != null && comand.Answers != null)
				{
					var number = _random.Next(0, comand.Answers.Count+1);
					if(number == comand.Answers.Count)
					stickerId = (Sticker)comand.RandomAttachment(AttachmentTypes.Sticker);
				}
				var answer = comand.RandomAnswer();
				var message = BlankMessage;
				if(answer?.Value == null)
				{
					if(stickerId != null) message = AttachmentTypes.Sticker.ToString();

				}
				else
				{
					if(_stickerCommandExist)
					{
						stickerId = null;
						message = StickerMessage;
					}
					else
						message = answer.Value;
				}
				List<MediaAttachment> attachments = null;
				if(comand?.Attachments != null && !comand.Attachments.Select(x => x is Sticker).All(x => x))
				{
					attachments = comand.Attachments;
				}

				isSend = vk.Messages.Send(new MessagesSendParams
				{
					UserId = userId,
					Message = message,
					StickerId = stickerId == null ? null : (uint?)stickerId.Id.Value,
					Attachments = !_stickerCommandExist ? attachments : null,
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

		private Command CreateCommand(string message, Dictionary<AttachmentTypes, List<MediaAttachment>> media = null)
		{
			var commands = _baseCommands.LoadAllCommands();
			var newCommand = !string.IsNullOrWhiteSpace(message) ? message.Substring(1, message.Length - 2).Split('~') : null;

			if(_currentStickerCommand != null && (media != null && media.TryGetValue(AttachmentTypes.Sticker, out var stiker)))
			{
				if(!_stickerCommandExist)
				{
					_currentStickerCommand.Attachments = stiker;
					_currentStickerCommand.Answers = null;
				}
				else
				{
					if(_currentStickerCommand.Attachments != null)
					{
						foreach(var item in stiker)
						{
							_currentStickerCommand.Attachments.Add(item);
						}
					}
					else
					{
						_currentStickerCommand.Attachments = stiker;
					}
				}
				return _baseCommands.AddComand(_currentStickerCommand);
			}

			if(newCommand.Length > 0)
			{
				Command commandExist;
				if(newCommand[0].ToLower().Equals(StickerCommandAdd))
				{
					commandExist = commands.FirstOrDefault(x => x.Value.Equals(newCommand[1].ToLower()));
					if(commandExist == null)
					{
						_currentStickerCommand = new Command
						{
							Value = newCommand[1].ToLower(),
							Answers = new List<Answer>
						{
							new Answer
							{
								Value = StickerMessage
							}
						}
						};
					}
					else
					{
						_stickerCommandExist = true;
						_currentStickerCommand = commandExist;
					}

					return _currentStickerCommand;
				}
				
				var answers = newCommand[1].Split('|');
				var answerList = new List<Answer>();
				if(answers.Length > 0)
				{
					foreach(var newAnswer in answers)
					{
						if(string.IsNullOrWhiteSpace(newAnswer)) continue;
						answerList.Add(new Answer
						{
							Value = newAnswer,
						});
					}
				}

				commandExist = commands.FirstOrDefault(x => x.Value.Equals(newCommand[0].ToLower()));
				Command result;
				if(commandExist != null)
				{
					if(answerList.Count > 0)
					{
						if(commandExist.Answers != null)
						{
							foreach(var value in answerList)
							{
								commandExist.Answers.Add(value);

							}
						}
						else
						{
							commandExist.Answers = answerList;
						}
					}

					if(media != null && media.Count > 0)
					{
						if(commandExist.Attachments != null)
						{
							foreach(var item in media)
							{
								foreach(var value in item.Value)
								{
									commandExist.Attachments.Add(value);
								}
							}
						}
						else
						{
							commandExist.Attachments = media.Select(x => x.Value).FirstOrDefault();
						}
					}

					result = _baseCommands.AddComand(commandExist);
				}
				else
				{
					result = _baseCommands.AddComand(new Command
					{
						Value = newCommand[0].ToLower(),
						Answers = answerList?.Count > 0 ? answerList : null,
						Attachments = media?.Select(x => x.Value)?.FirstOrDefault(),
					});
				}
				if(result != null)
				{
					return result;
				}
			}

			return null;
		}

		private Dictionary<AttachmentTypes, List<MediaAttachment>> ParsingAttachmets(object jsonAttachment)
		{
			var json = (JObject)jsonAttachment;
			var list = new List<VkResponse>(json.Count);
			foreach(var v in json)
			{
				list.Add(new VkResponse(v.Value));
			}

			var media = new List<MediaAttachment>();
			if(list.Exists(x => x.ToString().Equals("audio")))
			{
				foreach(var audio in list)
				{
					var parse = audio.ToString().Trim('{', '}').Split('_');
					if(parse.Length > 1)
					{
						media.Add(new Audio { OwnerId = long.Parse(parse[0]), Id = long.Parse(parse[1]) });
					}
				}

				return new Dictionary<AttachmentTypes, List<MediaAttachment>>
				{
					{ AttachmentTypes.Audio, media },
				};
			}
			else if(list.Exists(x => x.ToString().Equals("sticker")))
			{
				media.Add(new Sticker { ProductId = list[0], Id = list[2] });

				return new Dictionary<AttachmentTypes, List<MediaAttachment>>
				{
					{ AttachmentTypes.Sticker, media},
				};
			}

			return null;
		} 
	}
}
