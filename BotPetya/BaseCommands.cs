using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VkNet.Model.Attachments;

namespace BotPetya
{
	class BaseCommands
	{
		private string _path = Path.Combine(Environment.CurrentDirectory, "baseComands.xml");

		public Command AddComand(Command comand)
		{
			var fileName = GetPathDataFile();
			Command isAdded = null;
			try
			{
				var doc = new XmlDocument();
				doc.Load(fileName);

				XmlNode node = null;
				XmlNode answerNode = null;
				XmlNode attachmentsNode = null;
				XmlAttribute attr = null;

				var findCommand = doc.DocumentElement.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.Attributes[0].Value == comand.Value);
				if(findCommand != null)
				{
					node = findCommand;
				}
				else
				{
					node = doc.CreateElement("Command");
					attr = doc.CreateAttribute("Value");
					attr.Value = comand.Value;
					node.Attributes.Append(attr);
				}
				if(comand.Answers != null)
				{
					if(findCommand?["Answers"] != null)
					{
						answerNode = findCommand["Answers"];
					}
					else
					{
						answerNode = doc.CreateElement("Answers");
					}
					foreach(var answer in comand.Answers)
					{
						if(findCommand?["Answers"] != null)
						{
							var findAnswer = answerNode
								.ChildNodes
								.Cast<XmlNode>()
								.FirstOrDefault(x => x.Attributes[0].Value.Equals(answer.Value));
							if(findAnswer != null) continue;
						}
						answerNode.WriteParameter("Answer", answer.Value);
					}

					node.AppendChild(answerNode);
				}

				if(comand.Attachments != null)
				{
					var findAttachments = findCommand?["Attachments"];
					if(findAttachments != null)
					{
						attachmentsNode = findCommand["Attachments"];
					}
					else
					{
						attachmentsNode = doc.CreateElement("Attachments");
					}

					foreach(var attachment in comand.Attachments)
					{
						if(findAttachments != null)
						{
							if(attachment is Sticker sticker)
							{
								var findAttachment = attachmentsNode
									.ChildNodes
									.Cast<XmlNode>()
									.FirstOrDefault(x => x.HasChildNodes && x.ReadParameter("Id", default(long)) == sticker.Id);
								if(findAttachment != null)
									continue;

								var stickerNode = doc.CreateElement("Sticker");
								stickerNode.WriteParameter("Id", sticker.Id.Value);
								stickerNode.WriteParameter("ProductId", sticker.ProductId.Value);
								attachmentsNode.AppendChild(stickerNode);
							}
						}
						else
						{
							if(attachment is Sticker sticker)
							{
								var stickerNode = doc.CreateElement("Sticker");
								stickerNode.WriteParameter("Id", sticker.Id.Value);
								stickerNode.WriteParameter("ProductId", sticker.ProductId.Value);
								attachmentsNode.AppendChild(stickerNode);
							}
						}
					}

					node.AppendChild(attachmentsNode);
				}

				doc.DocumentElement.AppendChild(node);
				doc.Save(fileName);
				isAdded = comand;
			}
			catch
			{
				isAdded = null;
			}
			
			return isAdded;
		}

		public Command LoadCommand(Command command)
		{
			var fileName = GetPathDataFile();
			var doc = new XmlDocument();
			doc.Load(fileName);
			var nodeList = doc.DocumentElement.ChildNodes;
			foreach(XmlNode commandNode in nodeList)
			{
				if(commandNode?.Attributes[0]?.Value != null)
				{
					if(commandNode.Attributes[0].Value.Equals(command.Value))
					{
						var answersNode = commandNode?["Answers"];
						List<Answer> answers = null;
						if(answersNode != null)
						{
							answers = new List<Answer>(answersNode.ChildNodes.Count);
							foreach(XmlNode answer in answersNode.ChildNodes)
							{
								if(answer?.Attributes?[0]?.Value != null)
								{
									var value = answer.Attributes[0].Value;
									if(string.IsNullOrWhiteSpace(value)) continue;

									answers.Add(new Answer
									{
										Value = value,
									});
								}
							}
						}

						var attachmentsNode = commandNode?["Attachments"];
						List<MediaAttachment> attachments = null;
						if(attachmentsNode != null)
						{
							attachments = new List<MediaAttachment>(attachmentsNode.ChildNodes.Count);
							foreach(XmlNode attachment in attachmentsNode.ChildNodes)
							{
								if(attachment?.Name != null)
								{
									if(attachment.Name.Equals(AttachmentTypes.Sticker.ToString()))
									{
										var id = attachment.ReadParameter("Id", default(long));
										var productId = attachment.ReadParameter("ProductId", default(long));
										if(id != default && productId != default)
										{
											attachments.Add(new Sticker
											{
												Id = id,
												ProductId = productId
											});
										}
									}
								}
							}
						}
						return (new Command
						{
							Value = commandNode.Attributes[0].Value,
							Answers = answers?.Count == 0 ? null : answers,
							Attachments = attachments?.Count == 0 ? null : attachments,
						});
					}
				}
				
			}

			return null;
		}

		public List<Command> LoadAllCommands()
		{
			var fileName = GetPathDataFile();
			var doc = new XmlDocument();
			doc.Load(fileName);
			var nodeList = doc.DocumentElement.ChildNodes;
			var commands = new List<Command>(nodeList.Count);
			foreach(XmlNode command in nodeList)
			{
				var answersNode = command?["Answers"];
				List<Answer> answers = null;
				if(answersNode != null)
				{
					answers = new List<Answer>(answersNode.ChildNodes.Count);
					foreach(XmlNode answer in answersNode.ChildNodes)
					{
						var value = answer.Attributes[0].Value;
						if(string.IsNullOrWhiteSpace(value)) continue;
						
						answers.Add(new Answer
						{
							Value = value,
						});
					}
				}

				var attachmentsNode = command?["Attachments"];
				List<MediaAttachment> attachments = null;
				if(attachmentsNode != null)
				{
					attachments = new List<MediaAttachment>(attachmentsNode.ChildNodes.Count);
					foreach(XmlNode attachment in attachmentsNode.ChildNodes)
					{
						if(attachment?.Name != null)
						{
							if(attachment.Name.Equals(AttachmentTypes.Sticker.ToString()))
							{
								var id = attachment.ReadParameter("Id", default(long));
								var productId = attachment.ReadParameter("ProductId", default(long));
								if(id != default && productId != default)
								{
									attachments.Add(new Sticker
									{
										Id = id,
										ProductId = productId
									});
								}
							}
						}
					}
				}

				commands.Add(new Command
				{
					Value = command.Attributes[0].Value,
					Answers = answers?.Count == 0 ? null : answers,
					Attachments = attachments?.Count == 0 ? null : attachments,
				});
			}

			return commands;
		}

		private string GetPathDataFile()
		{
			if(!File.Exists(_path))
			{
				using(var writer = XmlWriter.Create(_path))
				{
					writer.WriteStartElement("Base");
				}
			}
			return _path;
		}
	}
}
