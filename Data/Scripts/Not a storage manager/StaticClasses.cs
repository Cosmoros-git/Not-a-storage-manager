using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Logistics
{
    // All info in summaries.

    /// <summary>
    /// Provides static methods for categorizing and managing blocks within the game.
    /// </summary>
    /// <remarks>
    /// The <see cref="StaticBlockCategorizer"/> class offers methods to determine the type of a block,
    /// add blocks to appropriate collections, and manage block storage by adding or removing blocks from a centralized storage system.
    /// 
    /// These methods are designed to help categorize blocks based on their type and to perform operations like adding or removing blocks from lists or storage collections.
    /// This class is useful for organizing blocks in a grid, particularly when working with grids that have multiple types of functional blocks.
    /// 
    /// Example usage:
    /// <code>
    /// // Determine the block type
    /// string blockType;
    /// StaticBlockCategorizer.GetBlockType(fatBlock, out blockType);
    ///
    /// // Add a block to storage
    /// StaticBlockCategorizer.GetBlockTypeAndAddToStorage(fatBlock, storage);
    ///
    /// // Remove a block from storage
    /// StaticBlockCategorizer.GetBlockTypeAndRemoveFromStorage(fatBlock, storage);
    /// </code>
    /// </remarks>
    public static class StaticBlockCategorizer
    {
        /// <summary>
        /// Determines the type of the given block and outputs it as a string representing its interface type.
        /// </summary>
        /// <param name="fatBlock">The block to categorize.</param>
        /// <param name="type">
        /// The output string representing the block's type. Possible values:
        /// <list type="bullet">
        /// <item>"IMyAssembler"</item>
        /// <item>"IMyCargoContainer"</item>
        /// <item>"IMyRefinery"</item>
        /// <item>"IMyGasGenerator"</item>
        /// <item>"IMyConveyorSorter"</item>
        /// <item>"IMyShipConnector"</item>
        /// <item>null</item>
        /// </list>
        /// </param>
        public static void GetBlockType(IMyCubeBlock fatBlock, out string type)
        {
            switch (fatBlock.BlockDefinition.TypeIdString)
            {
                case "MyObjectBuilder_Assembler":
                    type = "IMyAssembler";
                    break;

                case "MyObjectBuilder_CargoContainer":
                    type = "IMyCargoContainer";
                    break;

                case "MyObjectBuilder_Refinery":
                    type = "IMyRefinery";
                    break;

                case "MyObjectBuilder_OxygenGenerator":
                    type = "IMyGasGenerator";
                    break;

                case "MyObjectBuilder_ConveyorSorter":
                    type = "IMyConveyorSorter";
                    break;

                case "MyObjectBuilder_ShipConnector":
                    type = "IMyShipConnector";
                    break;

                default:
                    type = null;
                    break;
            }
        }

        public static void GetBlockTypeAndAddToStorage(
            IMyCubeBlock fatBlock,
            BlockStorage storage)
        {
            switch (fatBlock.BlockDefinition.TypeIdString)
            {
                case "MyObjectBuilder_Assembler":
                    storage.Assemblers.AddBlock((IMyAssembler)fatBlock);
                    break;

                case "MyObjectBuilder_CargoContainer":
                    storage.CargoContainers.AddBlock((IMyCargoContainer)fatBlock);
                    break;

                case "MyObjectBuilder_Refinery":
                    storage.Refineries.AddBlock((IMyRefinery)fatBlock);
                    break;

                case "MyObjectBuilder_OxygenGenerator":
                    storage.GasGenerators.AddBlock((IMyGasGenerator)fatBlock);
                    break;

                case "MyObjectBuilder_ConveyorSorter":
                    storage.ConveyorSorters.AddBlock((IMyConveyorSorter)fatBlock);
                    break;

                case "MyObjectBuilder_ShipConnector":
                    storage.ShipConnectors.AddBlock((IMyShipConnector)fatBlock);
                    break;
            }
        }

        public static void GetBlockTypeAndRemoveFromStorage(
            IMyCubeBlock fatBlock,
            BlockStorage storage)
        {
            switch (fatBlock.BlockDefinition.TypeIdString)
            {
                case "MyObjectBuilder_Assembler":
                    storage.Assemblers.RemoveBlock(fatBlock.EntityId);
                    break;

                case "MyObjectBuilder_CargoContainer":
                    storage.CargoContainers.RemoveBlock(fatBlock.EntityId);
                    break;

                case "MyObjectBuilder_Refinery":
                    storage.Refineries.RemoveBlock(fatBlock.EntityId);
                    break;

                case "MyObjectBuilder_OxygenGenerator":
                    storage.GasGenerators.RemoveBlock(fatBlock.EntityId);
                    break;

                case "MyObjectBuilder_ConveyorSorter":
                    storage.ConveyorSorters.RemoveBlock(fatBlock.EntityId);
                    break;

                case "MyObjectBuilder_ShipConnector":
                    storage.ShipConnectors.RemoveBlock(fatBlock.EntityId);
                    break;
            }
        }
    }

    /// <summary>
    /// Provides static methods for serializing and deserializing <see cref="GridData"/> objects to and from XML format.
    /// </summary>
    /// <remarks>
    /// This static class contains methods to facilitate the storage and retrieval of <see cref="GridData"/> objects in XML format.
    /// It is designed to handle the serialization of grid data to a file in local storage and the deserialization of that data back into objects.
    /// 
    /// The class is intended to be used for managing grid-related data persistently across game sessions.
    /// 
    /// Example usage:
    /// <code>
    /// // Serialize data to XML
    /// GridDataSerializer.SerializeToXml(gridData_(UniqueGridId).xml, dataList);
    /// 
    /// // Deserialize data from XML
    /// var dataList = GridDataSerializer.DeserializeFromXml(gridData_(UniqueGridId).xml);
    /// </code>
    /// </remarks>
    public static class GridDataSerializer
    {
        /// <summary>
        /// Serializes a list of <see cref="GridData"/> objects to an XML file in the local storage for faster initialization of the mod.
        /// </summary>
        /// <param name="fileName">The name of the file to save the serialized XML data.</param>
        /// <param name="data">The list of <see cref="GridData"/> objects to be serialized and saved.</param>
        /// <remarks>
        /// This method converts the provided list of <see cref="GridData"/> objects into an XML format and writes it to a file in the mod's local storage.
        /// The file is stored in a location specific to the mod, ensuring that the data is isolated and managed within the context of the mod.
        /// 
        /// Example usage:
        /// <code>
        /// var dataList = new List<GridData>
        /// {
        ///     new GridData { ObjectTypeString = "Assembler", IdList = new List<long> { 12345, 67890 } },
        ///     new GridData { ObjectTypeString = "Refinery", IdList = new List<long> { 111213, 141516 } }
        /// };
        /// SerializeToXml("gridData_(UniqueGridId).xml", dataList);
        /// </code>
        /// </remarks>
        public static void SerializeToXml(string fileName, List<GridData> data)
        {
            var xmlData = MyAPIGateway.Utilities.SerializeToXML(data);
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(GridDataSerializer)))
            {
                writer.Write(xmlData);
            }
        }

        /// <summary>
        /// Deserializes a list of <see cref="GridData"/> objects from an XML file stored in local storage.
        /// </summary>
        /// <param name="fileName">The name of the file to read the XML data from.</param>
        /// <returns>
        /// A list of <see cref="GridData"/> objects deserialized from the specified XML file.
        /// If the file does not exist, an empty list is returned.
        /// </returns>
        /// <remarks>
        /// This method reads the contents of the specified XML file from the mod's local storage and deserializes it into a list of <see cref="GridData"/> objects.
        /// 
        /// Example usage:
        /// <code>
        /// var dataList = DeserializeFromXml("gridData_(UniqueGridId).xml);
        /// if (dataList.Any())
        /// {
        ///     // Process the deserialized data
        /// }
        /// </code>
        /// </remarks>
        public static List<GridData> DeserializeFromXml(string fileName)
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(fileName, typeof(GridDataSerializer)))
                return new List<GridData>();

            using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(fileName, typeof(GridDataSerializer)))
            {
                var xmlData = reader.ReadToEnd();
                return MyAPIGateway.Utilities.SerializeFromXML<List<GridData>>(xmlData);
            }
        }
    }

    public static class SettingsManager //TODO make this?
    {
    }
}