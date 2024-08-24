using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using System.Xml.Serialization;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Utils;
using System.Reflection;
using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using static VRage.Game.MyObjectBuilder_AmmoMagazineDefinition;

namespace Logistics
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class GetCraftersAndCraftees : MySessionComponentBase
    {
        private DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> _allDefinitions;

        public List<MyDefinitionBase> ComponentsDefinitions;
        public List<MyDefinitionBase> OresDefinitions;
        public List<MyDefinitionBase> IngotDefinitions;
        public List<MyDefinitionBase> AmmoDefinition;

        public List<DataClass> AssemblerData = new List<DataClass>();
        public List<DataClass> RefineryData = new List<DataClass>();
        public List<DataClass> OxygenGeneratorData = new List<DataClass>();
        public string BlueprintsForGuide;
        public static GetCraftersAndCraftees Instance { get; private set; }
        public override void LoadData()
        {
            base.LoadData();
            _allDefinitions = MyDefinitionManager.Static.GetAllDefinitions();
            GetBasicObjects();
            SaveDataToXml("CraftersData.xml");
            Instance = this;
        }


        public void GetBasicObjects()
        {
            ComponentsDefinitions = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Component)).ToList();
            OresDefinitions = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Ore)).ToList();
            IngotDefinitions = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Ingot)).ToList();
            AmmoDefinition = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_AmmoMagazine)).ToList();

            var assemblers = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Assembler)).ToList();
            var refineries = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Refinery)).ToList();
            var oxygenGenerators = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_OxygenGenerator))
                .ToList();


            GetAssemblerBlueprints(assemblers);
            GetRefineryBlueprints(refineries);
            GetOxygenBlueprints(oxygenGenerators);
            Instance = this;
        }

        private void GetAssemblerBlueprints(List<MyDefinitionBase> definitions)
        {
            foreach (var definition in definitions)
            {
                AssemblerData.Add(new DataClass(definition,definition.Id.TypeId.ToString()));
            }
        }
        private void GetRefineryBlueprints(List<MyDefinitionBase> definitions)
        {
            foreach (var definition in definitions)
            {
                RefineryData.Add(new DataClass(definition, definition.Id.TypeId.ToString()));
            }
        }
        private void GetOxygenBlueprints(List<MyDefinitionBase> definitions)
        {
            foreach (var definition in definitions)
            {
                OxygenGeneratorData.Add(new DataClass(definition, definition.Id.TypeId.ToString()));
            }
        }

        public void SaveDataToXml(string fileName)
        {
            var ProductionContent = new StringBuilder();
            var xmlContent = new StringBuilder();
            xmlContent.AppendLine("<Definitions>");

            // Serialize Components
            xmlContent.AppendLine("  <Components>");
            foreach (var component in ComponentsDefinitions)
            {
                var compont = (MyComponentDefinition) component;
                var name = compont.DisplayNameText;
                xmlContent.AppendLine($"    <Component SubtypeId=\"{EscapeXml(component.Id.SubtypeId.ToString())}\" SubtypeName=\"{EscapeXml(name)}\" />");
            }
            xmlContent.AppendLine("  </Components>");

            ProductionContent.AppendLine("  <Components>");
            foreach (var component in ComponentsDefinitions)
            {
                var compont = (MyComponentDefinition)component;
                var name = compont.DisplayNameText;
                ProductionContent.AppendLine($"    <Component SubtypeId=\"{EscapeXml(component.Id.SubtypeId.ToString())}\" SubtypeName=\"{EscapeXml(name)}\" />");
            }
            ProductionContent.AppendLine("  </Components>");


            // Serialize Ores
            xmlContent.AppendLine("  <Ores>");
            foreach (var ore in OresDefinitions)
            {
                var subtypeId = ore.Id.SubtypeId.ToString();
                var item = (MyPhysicalItemDefinition)ore;
                var displayName = item.DisplayNameText;
                xmlContent.AppendLine($"    <Ore SubtypeId=\"{EscapeXml(ore.Id.SubtypeId.ToString())}\" SubtypeName=\"{EscapeXml(displayName)}\" />");
            }
            xmlContent.AppendLine("  </Ores>");

            // Serialize Ores
            ProductionContent.AppendLine("  <Ores>");
            foreach (var ore in OresDefinitions)
            {
                var subtypeId = ore.Id.SubtypeId.ToString();
                var item = (MyPhysicalItemDefinition)ore;
                var displayName = item.DisplayNameText;
                ProductionContent.AppendLine($"    <Ore SubtypeId=\"{EscapeXml(ore.Id.SubtypeId.ToString())}\" SubtypeName=\"{EscapeXml(displayName)}\" />");
            }
            ProductionContent.AppendLine("  </Ores>");

            // Serialize Ingots
            xmlContent.AppendLine("  <Ingots>");
            foreach (var ingot in IngotDefinitions)
            {
                var subtypeId = ingot.Id.SubtypeId.ToString();
                var item = (MyPhysicalItemDefinition)ingot;
                var displayName = item.DisplayNameText;
                xmlContent.AppendLine($"    <Ingot SubtypeId=\"{EscapeXml(subtypeId)}\" SubtypeName=\"{EscapeXml(displayName)}\" />");

            }
            xmlContent.AppendLine("  </Ingots>");


            ProductionContent.AppendLine("  <Ingots>");
            foreach (var ingot in IngotDefinitions)
            {
                var subtypeId = ingot.Id.SubtypeId.ToString();
                var item = (MyPhysicalItemDefinition)ingot;
                var displayName = item.DisplayNameText;
                ProductionContent.AppendLine($"    <Ingot SubtypeId=\"{EscapeXml(subtypeId)}\" SubtypeName=\"{EscapeXml(displayName)}\" />");

            }
            ProductionContent.AppendLine("  </Ingots>");


            xmlContent.AppendLine("  <Ammo>");
            foreach (var ammo in AmmoDefinition)
            {
                var subtypeId = ammo.Id.SubtypeId.ToString();
                var item = (MyPhysicalItemDefinition)ammo;
                var displayName = item.DisplayNameText;
                xmlContent.AppendLine($"    <Ammo SubtypeId=\"{EscapeXml(subtypeId)}\" SubtypeName=\"{EscapeXml(displayName)}\" />");

            }
            xmlContent.AppendLine("  </Ammo>");

            ProductionContent.AppendLine("  <Ammo>");
            foreach (var ammo in AmmoDefinition)
            {
                var subtypeId = ammo.Id.SubtypeId.ToString();
                var item = (MyPhysicalItemDefinition)ammo;
                var displayName = item.DisplayNameText;
                ProductionContent.AppendLine($"    <Ammo SubtypeId=\"{EscapeXml(subtypeId)}\" SubtypeName=\"{EscapeXml(displayName)}\" />");

            }
            ProductionContent.AppendLine("  </Ammo>");

            BlueprintsForGuide = ProductionContent.ToString();

            // Serialize Assembler Data
            xmlContent.AppendLine("  <Assemblers>");
            foreach (var assembler in AssemblerData)
            {
                xmlContent.AppendLine($"    <Assembler MainType=\"{EscapeXml(assembler.MainType)}\" SubtypeId=\"{EscapeXml(assembler.SubtypeId)}\" DisplayName=\"{EscapeXml(assembler.DisplayName)}\">");
                xmlContent.AppendLine("      <BlueprintClassEntries>");
                foreach (var blueprintClass in assembler.BlueprintClassEntries)
                {
                    xmlContent.AppendLine($"        <BlueprintClass Name=\"{EscapeXml(blueprintClass.BlueprintClass)}\">");
                    foreach (var blueprint in blueprintClass.Consists)
                    {
                        xmlContent.AppendLine($"          <Blueprint Id=\"{EscapeXml(blueprint.Id)}\" HashId=\"{EscapeXml(blueprint.HashId)}\" BaseProductionTimeInSeconds=\"{blueprint.BaseProductionTimeInSeconds}\" ConsumptionPerSecond=\"{blueprint.ConsumptionPerSecond}\">");
                        xmlContent.AppendLine("            <Prerequisites>");
                        foreach (var item in blueprint.Prerequisite)
                        {
                            xmlContent.AppendLine($"              <Item Amount=\"{item.Amount}\" TypeId=\"{item.Id.TypeId}\" SubtypeId=\"{item.Id.SubtypeId}\" />");
                        }
                        xmlContent.AppendLine("            </Prerequisites>");
                        xmlContent.AppendLine("            <Results>");
                        foreach (var item in blueprint.Results)
                        {
                            xmlContent.AppendLine($"              <Item Amount=\"{item.Amount}\" TypeId=\"{item.Id.TypeId}\" SubtypeId=\"{item.Id.SubtypeId}\" />");
                        }
                        xmlContent.AppendLine("            </Results>");
                        xmlContent.AppendLine("          </Blueprint>");
                    }
                    xmlContent.AppendLine("        </BlueprintClass>");
                }
                xmlContent.AppendLine("      </BlueprintClassEntries>");
                xmlContent.AppendLine("    </Assembler>");
            }
            xmlContent.AppendLine("  </Assemblers>");

            // Serialize Refinery Data
            xmlContent.AppendLine("  <Refineries>");
            foreach (var refinery in RefineryData)
            {
                xmlContent.AppendLine($"    <refinery MainType=\"{EscapeXml(refinery.MainType)}\" SubtypeId=\"{EscapeXml(refinery.SubtypeId)}\" DisplayName=\"{EscapeXml(refinery.DisplayName)}\">");
                xmlContent.AppendLine("      <BlueprintClassEntries>");
                foreach (var blueprintClass in refinery.BlueprintClassEntries)
                {
                    xmlContent.AppendLine($"        <BlueprintClass Name=\"{EscapeXml(blueprintClass.BlueprintClass)}\">");
                    foreach (var blueprint in blueprintClass.Consists)
                    {
                        xmlContent.AppendLine($"          <Blueprint Id=\"{EscapeXml(blueprint.Id)}\" HashId=\"{EscapeXml(blueprint.HashId)}\" BaseProductionTimeInSeconds=\"{blueprint.BaseProductionTimeInSeconds}\" ConsumptionPerSecond=\"{blueprint.ConsumptionPerSecond}\">");
                        xmlContent.AppendLine("            <Prerequisites>");
                        foreach (var item in blueprint.Prerequisite)
                        {
                            xmlContent.AppendLine($"              <Item Amount=\"{item.Amount}\" TypeId=\"{item.Id.TypeId}\" SubtypeId=\"{item.Id.SubtypeId}\" />");
                        }
                        xmlContent.AppendLine("            </Prerequisites>");
                        xmlContent.AppendLine("            <Results>");
                        foreach (var item in blueprint.Results)
                        {
                            xmlContent.AppendLine($"              <Item Amount=\"{item.Amount}\" TypeId=\"{item.Id.TypeId}\" SubtypeId=\"{item.Id.SubtypeId}\" />");
                        }
                        xmlContent.AppendLine("            </Results>");
                        xmlContent.AppendLine("          </Blueprint>");
                    }
                    xmlContent.AppendLine("        </BlueprintClass>");
                }
                xmlContent.AppendLine("      </BlueprintClassEntries>");
                xmlContent.AppendLine("    </Refinery>");
            }
            xmlContent.AppendLine("  </Refineries>");

            // Serialize Oxygen Generator Data
            xmlContent.AppendLine("  <OxygenGenerators>");
            foreach (var generator in OxygenGeneratorData)
            {
                xmlContent.AppendLine($"    <OxygenGenerator MainType=\"{EscapeXml(generator.MainType)}\" SubtypeId=\"{EscapeXml(generator.SubtypeId)}\" DisplayName=\"{EscapeXml(generator.DisplayName)}\">");
                xmlContent.AppendLine("      <BlueprintClassEntries>");
                foreach (var blueprintClass in generator.BlueprintClassEntries)
                {
                    xmlContent.AppendLine($"        <BlueprintClass Name=\"{EscapeXml(blueprintClass.BlueprintClass)}\">");
                    foreach (var blueprint in blueprintClass.Consists)
                    {
                        xmlContent.AppendLine($"          <Blueprint Id=\"{EscapeXml(blueprint.Id)}\" HashId=\"{EscapeXml(blueprint.HashId)}\" BaseProductionTimeInSeconds=\"{blueprint.BaseProductionTimeInSeconds}\" ConsumptionPerSecond=\"{blueprint.ConsumptionPerSecond}\">");
                        xmlContent.AppendLine("            <Prerequisites>");
                        foreach (var item in blueprint.Prerequisite)
                        {
                            xmlContent.AppendLine($"              <Item Amount=\"{item.Amount}\" TypeId=\"{item.Id.TypeId}\" SubtypeId=\"{item.Id.SubtypeId}\" />");
                        }
                        xmlContent.AppendLine("            </Prerequisites>");
                        xmlContent.AppendLine("            <Results>");
                        foreach (var item in blueprint.Results)
                        {
                            xmlContent.AppendLine($"              <Item Amount=\"{item.Amount}\" TypeId=\"{item.Id.TypeId}\" SubtypeId=\"{item.Id.SubtypeId}\" />");
                        }
                        xmlContent.AppendLine("            </Results>");
                        xmlContent.AppendLine("          </Blueprint>");
                    }
                    xmlContent.AppendLine("        </BlueprintClass>");
                }
                xmlContent.AppendLine("      </BlueprintClassEntries>");
                xmlContent.AppendLine("    </OxygenGenerator>");
            }
            xmlContent.AppendLine("  </OxygenGenerators>");

            xmlContent.AppendLine("</Definitions>");

            // Write the content to a file using MyAPIGateway.Utilities
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(GetCraftersAndCraftees)))
            {
                writer.Write(xmlContent.ToString());
            }

            MyAPIGateway.Utilities.ShowMessage("SaveDataToXml", $"Data successfully saved to {fileName}");
        }

        private static string EscapeXml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }

    public class DataClass
    {
        public string MainType;
        public string SubtypeId;
        public string DisplayName;
        public MyStringHash SubtypeStringHash;
        public List<BlueprintDataClass> BlueprintClassEntries = new List<BlueprintDataClass>();

        public DataClass(MyDefinitionBase definition, string mainType)
        {
            MainType = mainType;
            var id = definition.Id;
            DisplayName = definition.DisplayNameString;
            SubtypeId = id.SubtypeName;
            SubtypeStringHash = id.SubtypeId;
            var productionBlock = (MyProductionBlockDefinition)definition;
            var productionBlockBlueprintClasses = productionBlock.BlueprintClasses;
            foreach (var blueprint in productionBlockBlueprintClasses)
            {
                BlueprintClassEntries.Add(new BlueprintDataClass(blueprint.Id.SubtypeName, blueprint));
            }
        }
    }

    public class BlueprintDataClass
    {
        public string BlueprintClass;
        public List<Blueprint> Consists = new List<Blueprint>();

        public BlueprintDataClass(string blueprintClass, MyBlueprintClassDefinition leClass)
        {
            BlueprintClass = blueprintClass;
            foreach (var blueprint in leClass)
            {
                var subTypeName = blueprint.Id.SubtypeName;
                var subTypeHash = blueprint.Id.SubtypeId;
                var prerequisite = blueprint.Prerequisites;
                var results = blueprint.Results;
                var timeInSeconds = blueprint.BaseProductionTimeInSeconds;
                Consists.Add(new Blueprint(subTypeName, subTypeHash, prerequisite, results, timeInSeconds));
            }
        }
    }

    public class Blueprint
    {
        public string Id;
        public string HashId;
        public MyBlueprintDefinitionBase.Item[] Prerequisite;
        public MyBlueprintDefinitionBase.Item[] Results;
        public float BaseProductionTimeInSeconds;
        public float ConsumptionPerSecond;

        public Blueprint(string subTypeName, MyStringHash subTypeHash, MyBlueprintDefinitionBase.Item[] prerequisite,
            MyBlueprintDefinitionBase.Item[] results, float baseProductionTimeInSeconds)
        {
            Id = subTypeName;
            HashId = subTypeHash.ToString();
            Prerequisite = prerequisite;
            Results = results;
            BaseProductionTimeInSeconds = baseProductionTimeInSeconds;
            ConsumptionPerSecond = 1 / baseProductionTimeInSeconds;
        }
    }
}