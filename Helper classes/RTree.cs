using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Scenes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;

namespace INFOGR2024Template.Helper_classes;

/// <summary>
/// This is a R*-tree
/// https://en.wikipedia.org/wiki/R*-tree
/// </summary>
public class RTree
{
    private IScene scene { get; } //Used to get a reference to the list of primitives, otherwise we need an entire copy of the data structure
    private TreeNode rootNode { get; }
    private List<Sphere> spherePrimitives => scene.SpherePrimitives;
    private List<Triangle> trianglePrimitives => scene.TrianglePrimitives;
    private int maximumChildNodes { get; } = 10; //Maximum amount of child nodes stored inside of 1 node

    private int maximumValuesPerNode { get; } = 7; //Maximum amount of values that can be stored in 1 node before it overflows
    //This holds the method to turn the treeNodes into a single array to pass as a buffer to OpenGL

    public RTree(IScene scene)
    {
        this.scene = scene;
        rootNode = new TreeNode(maximumChildNodes, maximumValuesPerNode, spherePrimitives, trianglePrimitives);
    }

    public float[] TurnIntoFloatArray()
    {
        List<float> floatList = new List<float>();

        return rootNode.TurnIntoFloatArray(floatList).ToArray();
    }

    public void AddPrimitive(int primitivePointer)
    {
        rootNode.AddElement(primitivePointer);
    }
    
    //A single node in the RTree
    //Can be a branch or a leaf
    //Private as only a helper class for RTree and should not be accessed in any other way
    private class TreeNode
    {
        public bool isEmpty;
        public bool isLeaf;
        public TreeNode[] children; //Other nodes lower than this node
        private int maximumChildNodes { get; }
        public int[] primitivePointers; //If there are no other lower nodes, then store the resulting primitives
        public BoundingBox boundingBox;
        private List<Sphere> spherePrimitives { get; }
        private List<Triangle> trianglePrimitives { get; }
        private TreeNode parent;

        public TreeNode(int maximumChildNodes, int maximumValuesPerNode, List<Sphere> spherePrimitives, List<Triangle> trianglePrimitives)
        {
            isEmpty = true;
            isLeaf = true;
            this.maximumChildNodes = maximumChildNodes;
            primitivePointers = new int[maximumValuesPerNode];
            this.spherePrimitives = spherePrimitives;
            this.trianglePrimitives = trianglePrimitives;
            for (int i = 0; i < maximumValuesPerNode; i++)
            {
                //The 'null' value of the pointer
                primitivePointers[i] = -1;
            }
            //Update the bounding box (which will be empty) to make sure it at least has a value
            UpdateBoundingBox();
        }

        private IPrimitive GetPrimitive(int primitivePointer)
        {
            if (primitivePointer < spherePrimitives.Count)
            {
                return spherePrimitives[primitivePointer];
            }

            return trianglePrimitives[primitivePointer - spherePrimitives.Count];
        }

        /// <summary>
        /// Creates a list of floats that describe the data structure
        /// The list looks like this:
        /// 0-5 = boundingBoxValues (X, then Y, then Z. Min values, then max values)
        /// 6 = bool 0 or 1, 1 if it is a leaf and 0 if a branch
        /// In case it is a leaf
        /// -> 7 = amount of pointers to primitives
        /// -> 8-n = the pointer values (These are the values in the primitive list)
        /// Else in case it is a branch
        /// -> 7 = amount of child nodes
        /// -> 8-n = the indices where the other nodes start (so pointers)
        /// </summary>
        /// <param name="floatList"></param>
        /// <returns></returns>
        public List<float> TurnIntoFloatArray(List<float> floatList)
        {
            //Don't waste data on empty lists
            if (isEmpty)
                return floatList;
            
            //Add boundingBox
            floatList.Add(boundingBox.MinimumValues.X);
            floatList.Add(boundingBox.MinimumValues.Y);
            floatList.Add(boundingBox.MinimumValues.Z);
            floatList.Add(boundingBox.MaximumValues.X);
            floatList.Add(boundingBox.MaximumValues.Y);
            floatList.Add(boundingBox.MaximumValues.Z);
        
            //Add bool of branch or leaf
            floatList.Add(isLeaf ? 1 : 0);
            if (isLeaf)
            {
                //Checks how many pointers are in the list
                int count = 0;
                foreach (int primitivePointer in primitivePointers)
                {
                    if (primitivePointer != -1)
                        count++;
                }
                
                //Adds the set pointers to the float array
                floatList.Add(count);
                foreach (int primitivePointer in primitivePointers)
                {
                    if(primitivePointer != -1)
                        floatList.Add(primitivePointer);
                }
            }
            else
            {
                //First check how many pointers there are
                int currentIndex = floatList.Count; //First index where pointer to node will be placed
                int count = 0;
                floatList.Add(0); //This value will be adjusted later
                foreach (TreeNode treeNode in children)
                {
                    if (treeNode.boundingBox.GetSize() >= 0)
                    {
                        count++;
                        floatList.Add(0); //This value will be adjusted later
                    }
                }

                floatList[currentIndex] = count;

                //Actually add the pointers to the float array
                for (int i = 0; i < count; ) //Note doesn't increment!
                {
                    floatList[currentIndex + 1 + i] = floatList.Count;
                    
                    //Run entire algorithm for that node
                    if (children[i].boundingBox.GetSize() >= 0)
                    {
                        //Add the float list of it's children to this, the child starts off with an empty array
                        floatList.AddRange(children[i].TurnIntoFloatArray(new List<float>()));
                        i++;
                    }
                }
            }
            
            return floatList;
        }
        
        public void AddElement(int primitivePointer)
        {
            isEmpty = false;
            
            if (isLeaf)
            {
                //Find the first empty slot
                for (int i = 0; i < primitivePointers.Length; i++)
                {
                    if (primitivePointers[i] == -1)
                    {
                        //Add pointer to slot, then exit out
                        primitivePointers[i] = primitivePointer;
                        UpdateBoundingBox();
                        return;
                    }
                }
                
                //If there is no valid spot, then turn the node into a branch and give it leaves (which is splitting)
                SplitNode();
                
                //Then try inserting again
                AddElement(primitivePointer);
            }
            else //In case it is a branch
            {
                BoundingBox boundingBoxOfPrimitive = GetPrimitive(primitivePointer).BoundingBox;
                
                //If it is already fully in a boundingBox, add it to there
                foreach (TreeNode treeNode in children)
                {
                    if (boundingBox.FullyContains(boundingBoxOfPrimitive))
                    {
                        treeNode.AddElement(primitivePointer);
                        UpdateBoundingBox();
                        return;
                    }
                }
                
                //Otherwise
                //Make a new list and remove all that would overlap or exceed max size by adding the new primitive
                //Get the smallest increment from the trimmed list
                //If the trimmed list is empty add to something else

                List<int> childrenToIgnore = new List<int>();
                for (int i = 0; i < children.Length; i++)
                {
                    //Ignore boundingBoxes that would get a too large size
                    float value = children[i].boundingBox.CalculatePotentialBoundingBoxSize(boundingBoxOfPrimitive);
                    if (value > boundingBox.GetSize() / (maximumChildNodes * 0.75f)) //0.75f is heuristic
                    {
                        childrenToIgnore.Add(i);
                        continue; //Already added, so don't need to check further
                    }

                    //Check if the boundingBox would overlap with another if the primitive was added
                    BoundingBox tempBox = children[i].boundingBox.CalculatePotentialBoundingBox(boundingBoxOfPrimitive);
                    for (int j = 0; j < children.Length; j++)
                    {
                        if (i == j)
                            continue;

                        if (tempBox.Overlap(children[j].boundingBox))
                        {
                            childrenToIgnore.Add(i);
                        }
                    }
                }
                
                int index = -1;

                //If we don't need to ignore every child
                if (childrenToIgnore.Count < children.Length)
                {
                    //Choose the bounding box that will grow the least in size
                    float smallestSize = float.MaxValue;
                    
                    for (int i = 0; i < children.Length; i++)
                    {
                        //Don't count the ones that should be ignored
                        if (childrenToIgnore.Contains(i))
                            continue;
                        
                        //NOTE empty bounding boxes have negative size, so clamp to 0
                        float value = children[i].boundingBox.CalculatePotentialBoundingBoxSize(boundingBoxOfPrimitive) - MathF.Max(0, children[i].boundingBox.GetSize());
                        if (value < smallestSize)
                        {
                            smallestSize = value;
                            index = i;
                        }
                    }
                }
                else //Just check with everything, because everything is a bad choice
                {
                    //Choose the bounding box that will grow the least in size
                    float smallestSize = float.MaxValue;
                    
                    for (int i = 0; i < children.Length; i++)
                    {
                        //NOTE empty bounding boxes have negative size, so clamp to 0
                        float value = children[i].boundingBox.CalculatePotentialBoundingBoxSize(boundingBoxOfPrimitive) - MathF.Max(0, children[i].boundingBox.GetSize());
                        if (value < smallestSize)
                        {
                            smallestSize = value;
                            index = i;
                        }
                    }
                }
                
                //Add the element
                children[index].AddElement(primitivePointer);
                UpdateBoundingBox();
            }
        }

        //Turns this into a branch
        //Only called when we already know that the node will overflow
        private void SplitNode()
        {
            //Setup to turn in into a branch
            isLeaf = false;
            children = new TreeNode[maximumChildNodes];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = new TreeNode(maximumChildNodes, primitivePointers.Length, spherePrimitives, trianglePrimitives);
                children[i].parent = this;
            }
            
            //Place 2 furthest nodes together
            int index0 = -1;
            int index1 = -1;
            float distance = 0;

            //Get the 2 furthest nodes
            for (int i = 0; i < primitivePointers.Length; i++)
            {
                for (int j = 0; j < primitivePointers.Length; j++)
                {
                    //Don't check with itself
                    if (i == j)
                        continue;

                    IPrimitive primitive0 = GetPrimitive(primitivePointers[i]);
                    IPrimitive primitive1 = GetPrimitive(primitivePointers[j]);
                    
                    float smallX = MathF.Min(primitive0.BoundingBox.MinimumValues.X, primitive1.BoundingBox.MinimumValues.X);
                    float smallY = MathF.Min(primitive0.BoundingBox.MinimumValues.Y, primitive1.BoundingBox.MinimumValues.Y);
                    float smallZ = MathF.Min(primitive0.BoundingBox.MinimumValues.Z, primitive1.BoundingBox.MinimumValues.Z);
                    float bigX = MathF.Max(primitive0.BoundingBox.MaximumValues.X, primitive1.BoundingBox.MaximumValues.X);
                    float bigY = MathF.Max(primitive0.BoundingBox.MaximumValues.Y, primitive1.BoundingBox.MaximumValues.Y);
                    float bigZ = MathF.Max(primitive0.BoundingBox.MaximumValues.Z, primitive1.BoundingBox.MaximumValues.Z);

                    float newDistance = (bigX - smallX) * (bigY - smallY) * (bigZ - smallZ);

                    if (distance < newDistance)
                    {
                        distance = newDistance;
                        index0 = i;
                        index1 = j;
                    }
                }
            }
            
            //Assign the 2 furthest apart nodes as far away as possible
            children[0].AddElement(primitivePointers[index0]);
            children[1].AddElement(primitivePointers[index1]);
            
            //Throw away old list of pointers (but keep a copy until method is finished)
            int[] pointersCopy = new int[primitivePointers.Length];
            primitivePointers.CopyTo(pointersCopy, 0);
            primitivePointers = Array.Empty<int>();
            
            //Finally, just reinsert every node (except the 2 that were just added) into the parent, to redistribute
            for (int i = 0; i < pointersCopy.Length; i++)
            {
                if (i == index0 || i == index1)
                    continue;

                if (parent != null)
                {
                    if(pointersCopy[i] != -1)
                        parent.AddElement(pointersCopy[i]);
                }
                else
                {
                    if(pointersCopy[i] != -1)
                        AddElement(pointersCopy[i]);
                }
            }
        }
        
        private void UpdateBoundingBox()
        {
            float smallX = float.MaxValue;
            float smallY = float.MaxValue;
            float smallZ = float.MaxValue;
            float bigX = float.MinValue;
            float bigY = float.MinValue;
            float bigZ = float.MinValue;
            
            if (isLeaf)
            {
                foreach (int primitivePointer in primitivePointers)
                {
                    //-1 means not pointing to any primitive
                    if (primitivePointer == -1)
                        continue;
                    
                    smallX = MathF.Min(smallX, GetPrimitive(primitivePointer).BoundingBox.MinimumValues.X);
                    smallY = MathF.Min(smallY, GetPrimitive(primitivePointer).BoundingBox.MinimumValues.Y);
                    smallZ = MathF.Min(smallZ, GetPrimitive(primitivePointer).BoundingBox.MinimumValues.Z);
                    bigX = MathF.Max(bigX, GetPrimitive(primitivePointer).BoundingBox.MaximumValues.X);
                    bigY = MathF.Max(bigY, GetPrimitive(primitivePointer).BoundingBox.MaximumValues.Y);
                    bigZ = MathF.Max(bigZ, GetPrimitive(primitivePointer).BoundingBox.MaximumValues.Z);
                }
            }
            else //In the case it is a branch
            {
                foreach (TreeNode treeNode in children)
                {
                    //Overwrite any values than are less extreme than the new values in the child node
                    smallX = MathF.Min(smallX, treeNode.boundingBox.MinimumValues.X);
                    smallY = MathF.Min(smallY, treeNode.boundingBox.MinimumValues.Y);
                    smallZ = MathF.Min(smallZ, treeNode.boundingBox.MinimumValues.Z);
                    bigX = MathF.Max(bigX, treeNode.boundingBox.MaximumValues.X);
                    bigY = MathF.Max(bigY, treeNode.boundingBox.MaximumValues.Y);
                    bigZ = MathF.Max(bigZ, treeNode.boundingBox.MaximumValues.Z);
                }
            }
            
            //Update the bounding box
            boundingBox.MinimumValues = new Vector3(smallX, smallY, smallZ);
            boundingBox.MaximumValues = new Vector3(bigX, bigY, bigZ);
            
            //Update bounding box above this in tree hierarchy
            parent?.UpdateBoundingBox();
        }
    }
}