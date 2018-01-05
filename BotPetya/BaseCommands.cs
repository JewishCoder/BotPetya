using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
					if(findCommand?.FirstChild != null)
					{
						answerNode = findCommand.FirstChild;
					}
					else
					{
						answerNode = doc.CreateElement("Answers");
					}
					foreach(var answer in comand.Answers)
					{
						if(findCommand?.FirstChild != null)
						{
							var findAnswer = answerNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(x => x.Attributes[0].Value.Equals(answer.Value));
							if(findAnswer != null)
								continue;
						}
						answerNode.WriteParameter("Answer", answer.Value);
					}

					node.AppendChild(answerNode);
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
						var answersNode = commandNode.FirstChild;
						List<Answer> answers = null;
						if(answersNode != null)
						{
							answers = new List<Answer>(answersNode.ChildNodes.Count);
							foreach(XmlNode answer in answersNode.ChildNodes)
							{
								if(answer?.Attributes[0]?.Value != null)
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
						return (new Command
						{
							Value = commandNode.Attributes[0].Value,
							Answers = answers.Count == 0 ? null : answers,
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
				var answersNode = command.FirstChild;
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
				commands.Add(new Command
				{
					Value = command.Attributes[0].Value,
					Answers = answers.Count == 0 ? null : answers,
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
