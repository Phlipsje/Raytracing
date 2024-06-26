﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using INFOGR2024Template.SceneElements;
using System.Globalization;

namespace INFOGR2024Template.Helper_classes
{
    public static class OBJImportHelper
    {
        /// <summary>
        /// Imports exactly one model from an obj file, only supports faces made up of three verticies 
        /// </summary>
        /// <param name="filePath"></param>The path to an obj file containing just one model.
        /// <param name="scale"></param>The factor the model will be scaled with.
        /// <param name="Position"></param>The position the model will be placed in.
        /// <returns></returns>
        internal static Triangle[] ImportModel(string filePath, float scale, Vector3 Position, Material material)
        {
            List<Triangle> triangles = new List<Triangle>();
            List<Vector3> verticies = new List<Vector3>();
            StreamReader sr = new StreamReader(filePath);
            string line = sr.ReadLine();
            while (!sr.EndOfStream)
            {
                if (line != string.Empty)
                {
                    if (line[0] == 'v' && line[1] == ' ')
                    {
                        //adjust for some files using double spaces(for some curious reason)
                        if (line[2] != ' ')
                        {
                            string[] coordinates = line.Substring(2).Split(' ');
                            float x = float.Parse(coordinates[0], CultureInfo.InvariantCulture);
                            float y = float.Parse(coordinates[1], CultureInfo.InvariantCulture);
                            float z = float.Parse(coordinates[2], CultureInfo.InvariantCulture);
                            //xzy because we use y for up.
                            verticies.Add(Position + new Vector3(x, z, y) * scale);
                        }
                        else
                        {
                            string[] coordinates = line.Substring(3).Split("  ");
                            float x = float.Parse(coordinates[0], CultureInfo.InvariantCulture);
                            float y = float.Parse(coordinates[1], CultureInfo.InvariantCulture);
                            float z = float.Parse(coordinates[2], CultureInfo.InvariantCulture);
                            //xyz here because the double spaces documents also seem to the y up coordinate system
                            verticies.Add(Position + new Vector3(x, y, z) * scale);
                        }
                    }
                    else if (line[0] == 'f')
                    {
                        if (line[2] != ' ')
                        {
                            string[] vertexIndexes = line.Substring(2).Split(' ');
                            Vector3 vertex1 = verticies[int.Parse(vertexIndexes[0]) - 1];
                            Vector3 vertex2 = verticies[int.Parse(vertexIndexes[1]) - 1];
                            Vector3 vertex3 = verticies[int.Parse(vertexIndexes[2]) - 1];
                            triangles.Add(new Triangle(vertex1, vertex2, vertex3, material));
                        }
                        else
                        {
                            string[] vertexIndexes = line.Substring(3).Split("  ");
                            Vector3 vertex1 = verticies[int.Parse(vertexIndexes[0]) - 1];
                            Vector3 vertex2 = verticies[int.Parse(vertexIndexes[1]) - 1];
                            Vector3 vertex3 = verticies[int.Parse(vertexIndexes[2]) - 1];
                            triangles.Add(new Triangle(vertex1, vertex2, vertex3, material));
                        }
                    }
                }
                line = sr.ReadLine();
            }
            sr.Close();
            return triangles.ToArray();
        }
        /// <summary>
        /// returns the filepath for the name of the model when it is in the assets folder
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public static string FilePath(string modelName)
        {
            return "../../../assets/" + modelName + ".KarelModel";
        }
    }
}

