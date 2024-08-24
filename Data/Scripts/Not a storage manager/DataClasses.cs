using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Logistics
{

    /// <summary>
    /// Represents the storage structure for grid data in the application.
    /// </summary>
    /// <remarks>
    /// This class is used to store and manage data related to different types of objects within a grid.
    /// The data includes the type of object and a list of unique identifiers (IDs) associated with that object.
    /// 
    /// Example usage:
    /// The list of longs can also be populated by using IMyCubeBlock.EntityId
    /// <code>
    /// var gridData = new GridData
    /// {
    ///     ObjectTypeString = "Assembler",
    ///     IdList = new List<long> { 123456789, 987654321 }
    /// };
    /// </code>
    /// </remarks>
    [Serializable]
    public class GridData
    {
        /// <summary>
        /// Gets or sets the type of object represented by this data.
        /// </summary>
        /// <value>
        /// A string representing the type of object, such as "Assembler", "Refinery", etc.
        /// </value>
        public string ObjectTypeString { get; set; }

        /// <summary>
        /// Gets or sets the list of unique identifiers (IDs) associated with the object type.
        /// </summary>
        /// <value>
        /// A list of long integers, where each integer represents a unique ID for an object of the specified type.
        /// </value>
        public List<long> IdList { get; set; }
    }
    
    public class BlockTypeCollection<T> where T : IMyCubeBlock
    {
        private readonly List<T> _blockList;
        private readonly Dictionary<long, T> _blockDictionary;
        private readonly Dictionary<string, List<T>> _subtypeDictionary;

        public BlockTypeCollection(IEnumerable<T> blocks = null)
        {
            _blockList = blocks?.ToList() ?? new List<T>(); // Takes entry list of IMyCubeBlock
            _blockDictionary =
                _blockList.ToDictionary(block => block.EntityId); // Generates a dictionary with key being BlockID
            _subtypeDictionary = new Dictionary<string, List<T>>();

            foreach (var block in _blockList)
            {
                List<T> subtypeList;
                if (!_subtypeDictionary.TryGetValue(block.BlockDefinition.SubtypeName, out subtypeList))
                {
                    subtypeList = new List<T>();
                    _subtypeDictionary[block.BlockDefinition.SubtypeName] = subtypeList;
                }

                subtypeList.Add(block);
            }
        }

        public T GetBlockById(long entityId)
        {
            T block;
            _blockDictionary.TryGetValue(entityId, out block);
            return block;
        }

        public void AddBlock(T block)
        {
            if (_blockDictionary.ContainsKey(block.EntityId)) return;

            _blockList.Add(block);
            _blockDictionary[block.EntityId] = block;

            List<T> subtypeList;
            if (!_subtypeDictionary.TryGetValue(block.BlockDefinition.SubtypeName, out subtypeList))
            {
                subtypeList = new List<T>();
                _subtypeDictionary[block.BlockDefinition.SubtypeName] = subtypeList;
            }

            subtypeList.Add(block);
        }

        public bool RemoveBlock(long entityId)
        {
            T block;
            if (!_blockDictionary.TryGetValue(entityId, out block)) return false;

            _blockList.Remove(block);
            _blockDictionary.Remove(entityId);

            List<T> subtypeList;
            if (!_subtypeDictionary.TryGetValue(block.BlockDefinition.SubtypeName, out subtypeList)) return true;
            subtypeList.Remove(block);
            if (subtypeList.Count == 0)
            {
                _subtypeDictionary.Remove(block.BlockDefinition.SubtypeName);
            }

            return true;
        }

        public IEnumerable<T> GetBlocksBySubtype(string subtypeName)
        {
            List<T> blocks;
            return _subtypeDictionary.TryGetValue(subtypeName, out blocks) ? blocks : Enumerable.Empty<T>();
        }

        public IEnumerable<T> GetBlocks(Func<T, bool> predicate = null)
        {
            return predicate == null
                ? _blockList
                : // Return the entire list for enumeration
                _blockList.Where(predicate);
        }

        public IEnumerable<T> GetFunctionalBlocks()
        {
            return _blockList.Where(block => block.IsFunctional);
        }
        public int Count => _blockList.Count;
    }


}
