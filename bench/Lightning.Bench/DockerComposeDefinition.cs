﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using System.IO;

namespace DockerGenerator
{
	public class DockerComposeDefinition
	{
		public List<string> Fragments
		{
			get; set;
		}
		private string _Name;

		public DockerComposeDefinition(string name, List<string> fragments)
		{
			Fragments = fragments;
			_Name = name;
		}

		public string BuildOutput
		{
			get; set;
		}

		public void AddFragmentFile(string file)
		{
			Fragments.Add(File.ReadAllText(file));
		}

		public string GetFilePath()
		{
			return BuildOutput;
		}
		public void Build()
		{
			Console.WriteLine($"Generating {GetFilePath()}");
			var deserializer = new DeserializerBuilder().Build();
			var serializer = new SerializerBuilder().Build();

			Console.WriteLine($"With fragments:");
			foreach(var fragment in Fragments)
			{
				Console.WriteLine($"\t{fragment}");
			}
			var services = new List<KeyValuePair<YamlNode, YamlNode>>();
			var volumes = new List<KeyValuePair<YamlNode, YamlNode>>();

			foreach(var doc in Fragments.Select(f => ParseDocument(f)))
			{
				if(doc.Children.ContainsKey("services") && doc.Children["services"] is YamlMappingNode fragmentServicesRoot)
				{
					services.AddRange(fragmentServicesRoot.Children);
				}

				if(doc.Children.ContainsKey("volumes") && doc.Children["volumes"] is YamlMappingNode fragmentVolumesRoot)
				{
					volumes.AddRange(fragmentVolumesRoot.Children);
				}
			}

			
			YamlMappingNode output = new YamlMappingNode();
			output.Add("version", new YamlScalarNode("3") { Style = YamlDotNet.Core.ScalarStyle.DoubleQuoted });
			output.Add("services", new YamlMappingNode(Merge(services)));
			output.Add("volumes", new YamlMappingNode(volumes));
			var result = serializer.Serialize(output);
			var outputFile = GetFilePath();
			File.WriteAllText(outputFile, result.Replace("''", ""));
			Console.WriteLine($"Generated {outputFile}");
			Console.WriteLine();
		}

		private KeyValuePair<YamlNode, YamlNode>[] Merge(List<KeyValuePair<YamlNode, YamlNode>> services)
		{
				return services
					.GroupBy(s => s.Key.ToString(), s=> s.Value)
					.Select(group => 
						(GroupName: group.Key,  
						 MainNode: group.OfType<YamlMappingNode>().Single(n=> n.Children.ContainsKey("image")),
						 MergedNodes: group.OfType<YamlMappingNode>().Where(n => !n.Children.ContainsKey("image"))))
					.Select(_ =>
					{
						foreach(var node in _.MergedNodes)
						{
							foreach(var child in node)
							{
								var childValue = child.Value;
								if(!_.MainNode.Children.TryGetValue(child.Key, out var mainChildValue))
								{
									mainChildValue = child.Value;
									_.MainNode.Add(child.Key, child.Value);
								}
								else if(childValue is YamlMappingNode childMapping && mainChildValue is YamlMappingNode mainChildMapping)
								{
									foreach(var leaf in childMapping)
									{
										if(mainChildMapping.Children.TryGetValue(leaf.Key, out var mainLeaf))
										{
											if(leaf.Value is YamlScalarNode leafScalar && mainLeaf is YamlScalarNode leafMainScalar)
											{
												leafMainScalar.Value = leafMainScalar.Value + "," + leaf.Value;
											}
										}
										else
										{
											mainChildMapping.Add(leaf.Key, leaf.Value);
										}
									}
								}
								else if(childValue is YamlSequenceNode childSequence && mainChildValue is YamlSequenceNode mainSequence)
								{
									foreach(var c in childSequence.Children)
									{
										mainSequence.Add(c);
									}
								}
							}
						}
						return new KeyValuePair<YamlNode, YamlNode>(_.GroupName, _.MainNode);
					}).ToArray();
		}

		private YamlMappingNode ParseDocument(string fragment)
		{
			var input = new StringReader(fragment);
			YamlStream stream = new YamlStream();
			stream.Load(input);
			return (YamlMappingNode)stream.Documents[0].RootNode;
		}
	}
}
