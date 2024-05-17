using OpenTK.Mathematics;

namespace INFOGR2024Template.Helper_classes;

//A cube with the smallest possible size where everything can be stored in it
public struct BoundingBox
{
    public Vector3 minimumValues;
    public Vector3 maximumValues;

    public BoundingBox(Vector3 minimumValues, Vector3 maximumValues)
    {
        this.minimumValues = minimumValues;
        this.maximumValues = maximumValues;
    }

    public float GetSize()
    {
        float x = maximumValues.X - minimumValues.X;
        float y = maximumValues.Y - minimumValues.Y;
        float z = maximumValues.Z - minimumValues.Z;
        return  x * y * z;
    }
    
    public bool FullyContains(BoundingBox checkBoundingBox)
    {
        //Check for all false cases
        if (checkBoundingBox.minimumValues.X < minimumValues.X)
            return false;
        if (checkBoundingBox.minimumValues.Y < minimumValues.Y)
            return false;
        if (checkBoundingBox.minimumValues.Z < minimumValues.Z)
            return false;
        if (checkBoundingBox.maximumValues.X > maximumValues.X)
            return false;
        if (checkBoundingBox.maximumValues.Y > maximumValues.Y)
            return false;
        if (checkBoundingBox.maximumValues.Z > maximumValues.Z)
            return false;

        //Otherwise it is contained within the bounding box
        return true;
    }

    public float CalculatePotentialBoundingBoxSize(BoundingBox includedBoundingBox)
    {
        includedBoundingBox.minimumValues.X = MathF.Min(includedBoundingBox.minimumValues.X, minimumValues.X);
        includedBoundingBox.minimumValues.Y = MathF.Min(includedBoundingBox.minimumValues.Y, minimumValues.Y);
        includedBoundingBox.minimumValues.Z = MathF.Min(includedBoundingBox.minimumValues.Z, minimumValues.Z);
        includedBoundingBox.maximumValues.X = MathF.Max(includedBoundingBox.maximumValues.X, maximumValues.X);
        includedBoundingBox.maximumValues.Y = MathF.Max(includedBoundingBox.maximumValues.Y, maximumValues.Y);
        includedBoundingBox.maximumValues.Z = MathF.Max(includedBoundingBox.maximumValues.Z, maximumValues.Z);

        return includedBoundingBox.GetSize();
    }
    
    public BoundingBox CalculatePotentialBoundingBox(BoundingBox includedBoundingBox)
    {
        includedBoundingBox.minimumValues.X = MathF.Min(includedBoundingBox.minimumValues.X, minimumValues.X);
        includedBoundingBox.minimumValues.Y = MathF.Min(includedBoundingBox.minimumValues.Y, minimumValues.Y);
        includedBoundingBox.minimumValues.Z = MathF.Min(includedBoundingBox.minimumValues.Z, minimumValues.Z);
        includedBoundingBox.maximumValues.X = MathF.Max(includedBoundingBox.maximumValues.X, maximumValues.X);
        includedBoundingBox.maximumValues.Y = MathF.Max(includedBoundingBox.maximumValues.Y, maximumValues.Y);
        includedBoundingBox.maximumValues.Z = MathF.Max(includedBoundingBox.maximumValues.Z, maximumValues.Z);

        return includedBoundingBox;
    }

    public bool Overlap(BoundingBox boundingBox)
    {
        //Get all cases in which the boxes don't overlap
        if (boundingBox.minimumValues.X > maximumValues.X)
            return false;
        if (boundingBox.minimumValues.Y > maximumValues.Y)
            return false;
        if (boundingBox.minimumValues.Z > maximumValues.Z)
            return false;
        if (boundingBox.maximumValues.X < minimumValues.X)
            return false;
        if (boundingBox.maximumValues.Y < minimumValues.Y)
            return false;
        if (boundingBox.maximumValues.Z < minimumValues.Z)
            return false;
        
        //Else return true
        return true;
    }
}