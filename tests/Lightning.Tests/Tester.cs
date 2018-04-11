using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lightning.Tests
{
	public class Tester : IDisposable
	{
		public static Tester Create([CallerMemberNameAttribute]string scope = null)
		{
			return new Tester(scope);
		}

		string _Directory;
		public Tester(string scope)
		{
			_Directory = scope;
		}

		List<IDisposable> leases = new List<IDisposable>();
		List<IActorTester> actors = new List<IActorTester>();

		public Lightning.Alice.AliceRunner Alice
		{
			get;
			private set;
		}
		public Lightning.Alice.AliceRunner Bob
		{
			get;
			private set;
		}
		public Lightning.Alice.AliceRunner Carol
		{
			get;
			private set;
		}

		public void Start()
		{
			if(Directory.Exists(_Directory))
				Utils.DeleteDirectory(_Directory);
			if(!Directory.Exists(_Directory))
				Directory.CreateDirectory(_Directory);

			Alice = CreateActor<Lightning.Alice.AliceRunner, Lightning.Alice.AliceStartup>("Alice");
			Bob = CreateActor<Lightning.Alice.AliceRunner, Lightning.Alice.AliceStartup>("Bob");
			Carol = CreateActor<Lightning.Alice.AliceRunner, Lightning.Alice.AliceStartup>("Carol");

			foreach(var actor in actors)
			{
				actor.Start();
			}
		}

		int port = 24736;
		private TRunner CreateActor<TRunner, TStartup>(string name)
			where TRunner : Common.HostRunner<TStartup>, new()
			where TStartup : class
		{
			var actor = new ActorTester<TRunner, TStartup>(_Directory, name, port);
			actors.Add(actor);
			leases.Add(actor);
			port++;
			return actor.Runner;
		}

		public void Dispose()
		{
			foreach(var lease in leases)
				lease.Dispose();
		}
	}
}
