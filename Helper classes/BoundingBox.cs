using OpenTK.Mathematics;

namespace INFOGR2024Template.Helper_classes;

//A cube with the smallest possible size where everything can be stored in it
public struct BoundingBox
{
    public Vector3 MinimumValues;
    public Vector3 MaximumValues;

    public BoundingBox(Vector3 minimumValues, Vector3 maximumValues)
    {
        this.MinimumValues = minimumValues;
        this.MaximumValues = maximumValues;
    }

    public float GetSize()
    {
        float x = MaximumValues.X - MinimumValues.X;
        float y = MaximumValues.Y - MinimumValues.Y;
        float z = MaximumValues.Z - MinimumValues.Z;
        return  x * y * z;
    }
    
    public bool FullyContains(BoundingBox checkBoundingBox)
    {
        //Check for all false cases
        if (checkBoundingBox.MinimumValues.X < MinimumValues.X)
            return false;
        if (checkBoundingBox.MinimumValues.Y < MinimumValues.Y)
            return false;
        if (checkBoundingBox.MinimumValues.Z < MinimumValues.Z)
            return false;
        if (checkBoundingBox.MaximumValues.X > MaximumValues.X)
            return false;
        if (checkBoundingBox.MaximumValues.Y > MaximumValues.Y)
            return false;
        if (checkBoundingBox.MaximumValues.Z > MaximumValues.Z)
            return false;

        //Otherwise it is contained within the bounding box
        return true;
    }

    public float CalculatePotentialBoundingBoxSize(BoundingBox includedBoundingBox)
    {
        includedBoundingBox.MinimumValues.X = MathF.Min(includedBoundingBox.MinimumValues.X, MinimumValues.X);
        includedBoundingBox.MinimumValues.Y = MathF.Min(includedBoundingBox.MinimumValues.Y, MinimumValues.Y);
        includedBoundingBox.MinimumValues.Z = MathF.Min(includedBoundingBox.MinimumValues.Z, MinimumValues.Z);
        includedBoundingBox.MaximumValues.X = MathF.Max(includedBoundingBox.MaximumValues.X, MaximumValues.X);
        includedBoundingBox.MaximumValues.Y = MathF.Max(includedBoundingBox.MaximumValues.Y, MaximumValues.Y);
        includedBoundingBox.MaximumValues.Z = MathF.Max(includedBoundingBox.MaximumValues.Z, MaximumValues.Z);

        return includedBoundingBox.GetSize();
    }
    
    public BoundingBox CalculatePotentialBoundingBox(BoundingBox includedBoundingBox)
    {
        includedBoundingBox.MinimumValues.X = MathF.Min(includedBoundingBox.MinimumValues.X, MinimumValues.X);
        includedBoundingBox.MinimumValues.Y = MathF.Min(includedBoundingBox.MinimumValues.Y, MinimumValues.Y);
        includedBoundingBox.MinimumValues.Z = MathF.Min(includedBoundingBox.MinimumValues.Z, MinimumValues.Z);
        includedBoundingBox.MaximumValues.X = MathF.Max(includedBoundingBox.MaximumValues.X, MaximumValues.X);
        includedBoundingBox.MaximumValues.Y = MathF.Max(includedBoundingBox.MaximumValues.Y, MaximumValues.Y);
        includedBoundingBox.MaximumValues.Z = MathF.Max(includedBoundingBox.MaximumValues.Z, MaximumValues.Z);

        return includedBoundingBox;
    }

    public bool Overlap(BoundingBox boundingBox)
    {
        //Get all cases in which the boxes don't overlap
        if (boundingBox.MinimumValues.X > MaximumValues.X)
            return false;
        if (boundingBox.MinimumValues.Y > MaximumValues.Y)
            return false;
        if (boundingBox.MinimumValues.Z > MaximumValues.Z)
            return false;
        if (boundingBox.MaximumValues.X < MinimumValues.X)
            return false;
        if (boundingBox.MaximumValues.Y < MinimumValues.Y)
            return false;
        if (boundingBox.MaximumValues.Z < MinimumValues.Z)
            return false;
        
        //Else return true
        return true;
    }

    public float OverlapSize(BoundingBox boundingBox)
    {
        if (!Overlap(boundingBox))
            return 0;
        
        float minXOverlap = MathF.Max(MinimumValues.X, boundingBox.MinimumValues.X);
        float minYOverlap = MathF.Max(MinimumValues.Y, boundingBox.MinimumValues.Y);
        float minZOverlap = MathF.Max(MinimumValues.Z, boundingBox.MinimumValues.Z);
        float maxXOverlap = MathF.Min(MaximumValues.X, boundingBox.MaximumValues.X);
        float maxYOverlap = MathF.Min(MaximumValues.Y, boundingBox.MaximumValues.Y);
        float maxZOverlap = MathF.Min(MaximumValues.Z, boundingBox.MaximumValues.Z);

        return (maxXOverlap - minXOverlap) * (maxYOverlap - minYOverlap) * (maxZOverlap - minZOverlap);
    }
}