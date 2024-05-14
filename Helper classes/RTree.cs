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
    private List<IPrimitive> primitives => scene.Primitives;
    private int maximumChildNodes { get; } //Maximum amount of child nodes stored inside of 1 node
    private int maximumValuesPerNode { get; } //Maximum amount of values that can be stored in 1 node before it overflows
    //This holds the method to turn the treeNodes into a single array to pass as a buffer to OpenGL

    public RTree(IScene scene)
    {
        this.scene = scene;
        rootNode = new TreeNode(maximumChildNodes, maximumValuesPerNode);
    }
    
    //A single node in the RTree
    //Can be a branch or a leaf
    //Private as only a helper class for RTree and should not be accessed in any other way
    private class TreeNode
    {
        private bool isLeaf;
        private TreeNode[] children; //Other nodes lower than this node
        private int maximumChildNodes { get; }
        private int[] primitivePointers; //If there are no other lower nodes, then store the resulting primitives
        private BoundingBox boundingBox;

        public TreeNode(int maximumChildNodes, int maximumValuesPerNode)
        {
            isLeaf = true;
            this.maximumChildNodes = maximumChildNodes;
            primitivePointers = new int[maximumValuesPerNode];
            for (int i = 0; i < maximumValuesPerNode; i++)
            {
                //The 'null' value of the pointer
                primitivePointers[i] = -1;
            }
            //Update the bounding box (which will be empty) to make sure it at least has a value
            UpdateBoundingBox();
        }
        
        private void AddElement()
        {
            
        }

        private void SplitNode()
        {
            
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
                    //TODO need Debug2D branch before can implement bounding box
                    //smallX = MathF.Min(smallX, primitives[primitivePointer].);
                }
            }
            else //In the case it is a branch
            {
                foreach (TreeNode treeNode in children)
                {
                    //Overwrite any values than are less extreme than the new values in the child node
                    smallX = MathF.Min(smallX, treeNode.boundingBox.minimumValues.X);
                    smallY = MathF.Min(smallY, treeNode.boundingBox.minimumValues.Y);
                    smallZ = MathF.Min(smallZ, treeNode.boundingBox.minimumValues.Z);
                    bigX = MathF.Max(bigX, treeNode.boundingBox.maximumValues.X);
                    bigY = MathF.Max(bigY, treeNode.boundingBox.maximumValues.Y);
                    bigZ = MathF.Max(bigZ, treeNode.boundingBox.maximumValues.Z);
                }
            }
            
            //Update the bounding box
            boundingBox.minimumValues = new Vector3(smallX, smallY, smallZ);
            boundingBox.maximumValues = new Vector3(bigX, bigY, bigZ);
        }
    }

    //A cube with the smallest possible size where everything can be stored in it
    private struct BoundingBox
    {
        public Vector3 minimumValues;
        public Vector3 maximumValues;

        public BoundingBox(Vector3 minimumValues, Vector3 maximumValues)
        {
            this.minimumValues = minimumValues;
            this.maximumValues = maximumValues;
        }
    }
}