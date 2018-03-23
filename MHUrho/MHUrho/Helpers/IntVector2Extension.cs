﻿using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Storage;

namespace MHUrho.Helpers
{
    public static class IntVector2Extension
    {
        public static StIntVector2 Save(this IntVector2 intVector2) {
            var storedIntVector2 = new StIntVector2() {
                                                          X = intVector2.X,
                                                          Y = intVector2.Y
                                                      };
            return storedIntVector2;
        }
    }
}