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
		public bool AddComand(Comand comand)
		{
			var fileName = Path.Combine(Environment.CurrentDirectory, "baseComands.xml");
			var isAdded = true;
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
			}
			catch
			{
				isAdded = false;
			}
			
			return isAdded;
		}

		public void LoadComand()
		{
		}

		public List<Comand> LoadAllComands()
		{
			var fileName = Path.Combine(Environment.CurrentDirectory, "baseComands.xml");
			var doc = new XmlDocument();
			doc.Load(fileName);
			var nodeList = doc.DocumentElement.ChildNodes;
			var commands = new List<Comand>(nodeList.Count);
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
				commands.Add(new Comand
				{
					Value = command.Attributes[0].Value,
					Answers = answers.Count == 0 ? null : answers,
				});
			}

			return commands;
		}
	}
}
