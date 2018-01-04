using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotPetya
{
	public class Message
	{
		public ulong Ts { get; set; }

		public List<List<object>> Updates { get; set; }
	}
}
