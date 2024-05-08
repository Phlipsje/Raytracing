﻿using OpenTK.SceneElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using INFOGR2024Template.SceneElements;

namespace INFOGR2024Template.Scenes
{
    /// <summary>
    /// The interface that represents a scene. Every scene has a list of primitives and a camera.
    /// </summary>
    public interface IScene
    {
        public List<IPrimitive> Primitives { get; set; }
        public List<PointLight> PointLights { get; set; }
        public Camera Camera { get; set; }

        public void Tick();
    }
}
