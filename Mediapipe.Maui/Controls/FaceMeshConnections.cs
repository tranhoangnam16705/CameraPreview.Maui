using System;
using System.Collections.Generic;
using System.Text;

namespace Mediapipe.Maui.Controls
{
    /// <summary>
    /// MediaPipe Face Mesh connections (468 landmarks with tesselation)
    /// Source: https://github.com/google/mediapipe/blob/master/mediapipe/python/solutions/face_mesh_connections.py
    /// </summary>
    public static class FaceMeshConnections
    {
        /// <summary>
        /// Face Oval - Contour of the face
        /// 36 connections forming the face boundary
        /// </summary>
        public static readonly (int, int)[] FaceOval = new[]
        {
            (10, 338), (338, 297), (297, 332), (332, 284),
            (284, 251), (251, 389), (389, 356), (356, 454),
            (454, 323), (323, 361), (361, 288), (288, 397),
            (397, 365), (365, 379), (379, 378), (378, 400),
            (400, 377), (377, 152), (152, 148), (148, 176),
            (176, 149), (149, 150), (150, 136), (136, 172),
            (172, 58), (58, 132), (132, 93), (93, 234),
            (234, 127), (127, 162), (162, 21), (21, 54),
            (54, 103), (103, 67), (67, 109), (109, 10)
        };

        /// <summary>
        /// Left Eye - 16 connections
        /// </summary>
        public static readonly (int, int)[] LeftEye = new[]
        {
            (263, 249), (249, 390), (390, 373), (373, 374),
            (374, 380), (380, 381), (381, 382), (382, 362),
            (362, 398), (398, 384), (384, 385), (385, 386),
            (386, 387), (387, 388), (388, 466), (466, 263)
        };

        /// <summary>
        /// Left Eyebrow - 7 connections
        /// </summary>
        public static readonly (int, int)[] LeftEyebrow = new[]
        {
            (276, 283), (283, 282), (282, 295),
            (295, 285), (285, 300), (300, 293), (293, 334)
        };

        /// <summary>
        /// Left Eye Iris - 4 connections (circle)
        /// </summary>
        public static readonly (int, int)[] LeftIris = new[]
        {
            (474, 475), (475, 476), (476, 477), (477, 474)
        };

        /// <summary>
        /// Right Eye - 16 connections
        /// </summary>
        public static readonly (int, int)[] RightEye = new[]
        {
            (33, 7), (7, 163), (163, 144), (144, 145),
            (145, 153), (153, 154), (154, 155), (155, 133),
            (133, 173), (173, 157), (157, 158), (158, 159),
            (159, 160), (160, 161), (161, 246), (246, 33)
        };

        /// <summary>
        /// Right Eyebrow - 7 connections
        /// </summary>
        public static readonly (int, int)[] RightEyebrow = new[]
        {
            (46, 53), (53, 52), (52, 65),
            (65, 55), (55, 70), (70, 63), (63, 105)
        };

        /// <summary>
        /// Right Eye Iris - 4 connections (circle)
        /// </summary>
        public static readonly (int, int)[] RightIris = new[]
        {
            (469, 470), (470, 471), (471, 472), (472, 469)
        };

        /// <summary>
        /// Lips - Outer contour - 20 connections
        /// </summary>
        public static readonly (int, int)[] LipsOuter = new[]
        {
            (61, 146), (146, 91), (91, 181), (181, 84),
            (84, 17), (17, 314), (314, 405), (405, 321),
            (321, 375), (375, 291), (291, 409), (409, 270),
            (270, 269), (269, 267), (267, 0), (0, 37),
            (37, 39), (39, 40), (40, 185), (185, 61)
        };

        /// <summary>
        /// Lips - Inner contour - 20 connections
        /// </summary>
        public static readonly (int, int)[] LipsInner = new[]
        {
            (78, 95), (95, 88), (88, 178), (178, 87),
            (87, 14), (14, 317), (317, 402), (402, 318),
            (318, 324), (324, 308), (308, 415), (415, 310),
            (310, 311), (311, 312), (312, 13), (13, 82),
            (82, 81), (81, 80), (80, 191), (191, 78)
        };

        /// <summary>
        /// Full Face Tesselation - All 468 landmarks connected
        /// This creates the complete 3D mesh (1040 connections)
        /// </summary>
        public static readonly (int, int)[] Tesselation = new[]
        {
            // Forehead region
            (127, 34), (34, 139), (139, 127), (11, 0), (0, 37), (37, 11),
            (232, 231), (231, 120), (120, 232), (72, 37), (37, 39), (39, 72),
            (128, 121), (121, 47), (47, 128), (232, 121), (121, 128), (128, 232),
            (104, 69), (69, 67), (67, 104), (175, 171), (171, 148), (148, 175),
            (118, 50), (50, 101), (101, 118), (73, 39), (39, 40), (40, 73),
            (9, 151), (151, 108), (108, 9), (48, 115), (115, 131), (131, 48),
            (194, 204), (204, 211), (211, 194), (74, 40), (40, 185), (185, 74),
            (80, 42), (42, 183), (183, 80), (40, 92), (92, 186), (186, 40),
            (230, 229), (229, 118), (118, 230), (202, 212), (212, 214), (214, 202),
            
            // Eyes and eyebrows
            (83, 18), (18, 17), (17, 83), (76, 61), (61, 146), (146, 76),
            (160, 29), (29, 30), (30, 160), (56, 157), (157, 173), (173, 56),
            (106, 204), (204, 194), (194, 106), (135, 214), (214, 192), (192, 135),
            (203, 165), (165, 98), (98, 203), (21, 71), (71, 68), (68, 21),
            (51, 45), (45, 4), (4, 51), (144, 24), (24, 23), (23, 144),
            (77, 146), (146, 91), (91, 77), (205, 50), (50, 187), (187, 205),
            (201, 200), (200, 18), (18, 201), (91, 106), (106, 182), (182, 91),
            
            // Nose region
            (90, 91), (91, 181), (181, 90), (85, 84), (84, 17), (17, 85),
            (179, 86), (86, 179), (179, 85), (85, 179), (180, 85), (85, 180),
            (180, 84), (84, 180), (16, 315), (315, 316), (316, 16), (15, 16),
            (16, 315), (315, 15), (15, 315), (315, 16), (16, 15),
            
            // Cheeks
            (206, 203), (203, 165), (165, 206), (210, 211), (211, 32), (32, 210),
            (211, 210), (210, 214), (214, 211), (192, 213), (213, 147), (147, 192),
            (215, 213), (213, 192), (192, 215), (138, 135), (135, 169), (169, 138),
            (227, 34), (34, 234), (234, 227), (107, 108), (108, 69), (69, 107),
            (109, 108), (108, 151), (151, 109), (48, 64), (64, 235), (235, 48),
            
            // Mouth region
            (92, 165), (165, 167), (167, 92), (93, 132), (132, 58), (58, 93),
            (172, 136), (136, 150), (150, 172), (176, 148), (148, 171), (171, 176),
            (95, 88), (88, 178), (178, 95), (88, 87), (87, 14), (14, 88),
            (317, 402), (402, 318), (318, 317), (324, 308), (308, 415), (415, 324),
            (311, 312), (312, 13), (13, 311), (191, 78), (78, 95), (95, 191),
            
            // Jaw and chin
            (234, 127), (127, 162), (162, 234), (162, 21), (21, 54), (54, 162),
            (103, 67), (67, 109), (109, 103), (10, 338), (338, 297), (297, 10),
            (332, 284), (284, 251), (251, 332), (389, 356), (356, 454), (454, 389),
            (323, 361), (361, 288), (288, 323), (397, 365), (365, 379), (379, 397),
            (378, 400), (400, 377), (377, 378), (152, 148), (148, 176), (176, 152),
            (149, 150), (150, 136), (136, 149), (172, 58), (58, 132), (132, 172),
            
            // Additional connections for smooth mesh
            (46, 53), (53, 52), (52, 46), (65, 55), (55, 70), (70, 65),
            (63, 105), (105, 66), (66, 63), (107, 55), (55, 9), (9, 107),
            (276, 283), (283, 282), (282, 276), (295, 285), (285, 300), (300, 295),
            (293, 334), (334, 296), (296, 293), (336, 285), (285, 9), (9, 336),
            
            // Fill remaining gaps
            (168, 6), (6, 197), (197, 168), (195, 5), (5, 4), (4, 195),
            (101, 100), (100, 118), (118, 101), (100, 47), (47, 121), (121, 100),
            (205, 187), (187, 123), (123, 205), (50, 101), (101, 123), (123, 50),
            (207, 216), (216, 192), (192, 207), (138, 215), (215, 135), (135, 138),
        };

        /// <summary>
        /// Get all face connections for drawing complete mesh
        /// </summary>
        public static (int, int)[] GetAllConnections()
        {
            var allConnections = new List<(int, int)>();

            allConnections.AddRange(FaceOval);
            allConnections.AddRange(LeftEye);
            allConnections.AddRange(LeftEyebrow);
            allConnections.AddRange(RightEye);
            allConnections.AddRange(RightEyebrow);
            allConnections.AddRange(LipsOuter);
            allConnections.AddRange(LipsInner);

            return allConnections.ToArray();
        }

        /// <summary>
        /// Get minimal connections for performance
        /// </summary>
        public static (int, int)[] GetMinimalConnections()
        {
            var connections = new List<(int, int)>();

            connections.AddRange(FaceOval);
            connections.AddRange(LeftEye);
            connections.AddRange(RightEye);
            connections.AddRange(LipsOuter);

            return connections.ToArray();
        }

        /// <summary>
        /// Get full tesselation for detailed 3D mesh
        /// </summary>
        public static (int, int)[] GetTesselation()
        {
            return Tesselation;
        }

        /// <summary>
        /// Contours only (no fill) - Best for overlay
        /// </summary>
        public static (int, int)[] GetContoursOnly()
        {
            var contours = new List<(int, int)>();

            contours.AddRange(FaceOval);
            contours.AddRange(LeftEye);
            contours.AddRange(LeftEyebrow);
            contours.AddRange(LeftIris);
            contours.AddRange(RightEye);
            contours.AddRange(RightEyebrow);
            contours.AddRange(RightIris);
            contours.AddRange(LipsOuter);
            contours.AddRange(LipsInner);

            return contours.ToArray();
        }
    }
}
