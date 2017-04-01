using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Component
{
    public class Functionality
    {
        /***
         * Common functions for #Functions
        ***/

        /********** Variables **********/
        public static int __TOTAL_FUNCTONAL_PATCHES = 35; // ncat * npatch
        public static int _NUM_FUNCTIONALITY = 6;
        public static int _NUM_CATEGORIY = 15;
        public static int _NUM_UNARY_FEATURE = 420;
        public static int _NUM_BINARY_FEATURE = 110;
        public static int[] _PAIR_INDEX_1 = { 0 }; // 1 functional patch
        public static int[] _PAIR_INDEX_2 = { 0, 2, 3 }; // 2 patches - 4 pairs
        public static int[] _PAIR_INDEX_3 = { 0, 3, 6, 5, 7, 8 }; // 3 patches - 6 pairs
        public static int _MAX_MATRIX_DIM = 300;
        public static int _MAX_TRY_TIMES = 60;

        public static int _MAX_GEN_HYBRID_NUMBER = 10;
        public static int _MAX_USE_PRESENT_NUMBER = 10;
        public static int _NUM_INTER_BEFORE_RERUN = 5;

        public static double _NOVELTY_MINIMUM = 0.5;
        public static double _NOVELTY_MAXIMUM = 1.0;

        public static int _POINT_FEAT_DIM = 3;
        public static int _CURV_FEAT_DIM = 4;
        public static int _PCA_FEAT_DIM = 5;
        public static int _RAY_FEAT_DIM = 2;
        public static int _CONVEXHULL_FEAT_DIM = 2;
        public static int _POINT_FEATURE_DIM = 18;

        public enum Functions { GROUND_TOUCHING, HUMAN_BACK, HUMAN_HIP, HAND_HOLD, HAND_PLACE, SUPPORT, HANG };

        public enum Category { Backpack, Basket, Bicycle, Chair, Desk, DryingRack, Handcart, Hanger, Hook, Shelf, 
            Stand, Stroller, Table, TVBench, Vase, None };


        /********** Functions **********/
        public Functionality() { }

        public static string getTopPredictedCategories(double[] scores)
        {
            int n = scores.Length;
            int mtop = 4;
            int[] index = new int[n];
            for (int i = 0; i < n; ++i)
            {
                index[i] = i;
            }
            Array.Sort(index, (a, b) => scores[b].CompareTo(scores[a]));
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < mtop; ++i)
            {
                sb.Append(getCategoryName(index[i]));
                sb.Append(" ");
            }
            return sb.ToString();
        }// getTopPredictedCategories

        public static bool isKnownCategory(int index)
        {
            return index >= 0 && index < 15;
        }

        public static bool IsMainFunction(Functions f)
        {
            return f == Functions.HAND_PLACE || f == Functions.HUMAN_HIP;
        }

        public static string getCategoryName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Backpack";
                case 1:
                    return "Basket";
                case 2:
                    return "Bicycle";
                case 3:
                    return "Chair";
                case 4:
                    return "Desk";
                case 5:
                    return "DryingRack";
                case 6:
                    return "Handcart";
                case 7:
                    return "Hanger";
                case 8:
                    return "Hook";
                case 9:
                    return "Shelf";
                case 10:
                    return "Stand";
                case 11:
                    return "Stroller";
                case 12:
                    return "Table";
                case 13:
                    return "TVBench";
                case 14:
                    return "Vase";
                default:
                    return "None";
            }
        }// getCategoryName

        public static Category getCategory(string str)
        {
            string sstr = str.ToLower();
            switch (sstr)
            {
                case "backpack":
                    return Category.Backpack;
                case "basket":
                    return Category.Basket;
                case "bicycle":
                    return Category.Bicycle;
                case "chair":
                    return Category.Chair;
                case "desk":
                    return Category.Desk;
                case "dryingrack":
                    return Category.DryingRack;
                case "handcart":
                    return Category.Handcart;
                case "hanger":
                    return Category.Hanger;
                case "hook":
                    return Category.Hook;
                case "shelf":
                    return Category.Shelf;
                case "stand":
                    return Category.Stand;
                case "stroller":
                    return Category.Stroller;
                case "table":
                    return Category.Table;
                case "tvbench":
                    return Category.TVBench;
                case "vase":
                    return Category.Vase;
                default:
                    return Category.None;
            }
        }// getCategory

        public static int getNumberOfFunctionalPatchesPerCategory(Category cat)
        {
            switch (cat)
            {
                case Category.Backpack:
                    return 1;
                case Category.Basket:
                    return 2;
                case Category.Bicycle:
                    return 2;
                case Category.Chair:
                    return 2;
                case Category.Desk:
                    return 3;
                case Category.DryingRack:
                    return 2;
                case Category.Handcart:
                    return 3;
                case Category.Hanger:
                    return 2;
                case Category.Hook:
                    return 2;
                case Category.Shelf:
                    return 3;
                case Category.Stand:
                    return 2;
                case Category.Stroller:
                    return 3;
                case Category.Table:
                    return 3;
                case Category.TVBench:
                    return 3;
                case Category.Vase:
                    return 2;
                default:
                    return 0;
            }
        }// getNumberOfFunctionalPatchesPerCategory

        public static int[] getCategoryPatchIndicesInFeatureVector(Functionality.Category cat)
        {
            int[] idxs = new int[getNumberOfFunctionalPatchesPerCategory(cat)];
            int catIdx = (int)cat;
            int start = 0;
            for (int i = 0; i < catIdx; ++i)
            {
                start += getNumberOfFunctionalPatchesPerCategory((Category)i);
            }
            for (int i = 0; i < idxs.Length; ++i)
            {
                idxs[i] = start + i;
            }
            return idxs;
        }// getCategoryPatchIndicesInFeatureVector

        public static List<Functions> getFunctionalityFromCategory(Category cat)
        {
            List<Functions> funcs = new List<Functions>();
            if (cat == Category.Chair)
            {
                funcs.Add(Functions.HUMAN_HIP);
                funcs.Add(Functions.GROUND_TOUCHING);
                funcs.Add(Functions.HUMAN_BACK);
            }
            if (cat == Category.Handcart)
            {
                funcs.Add(Functions.HAND_HOLD);
                funcs.Add(Functions.GROUND_TOUCHING);
                funcs.Add(Functions.HAND_PLACE);
            }
            if (cat == Category.Basket)
            {
                funcs.Add(Functions.HAND_HOLD);
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            if (cat == Category.Shelf)
            {
                funcs.Add(Functions.HAND_PLACE);
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            if (cat == Category.DryingRack)
            {
                funcs.Add(Functions.HAND_PLACE);
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            if (cat == Category.Stand)
            {
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            return funcs;
        }// getFunctionalityFromCategory

        public static List<Functions> getFunctionalityFromCategories(List<Category> cats)
        {
            List<Functions> funcs = new List<Functions>();
            foreach (Category cat in cats)
            {
                funcs.AddRange(getFunctionalityFromCategory(cat));
            }
            return funcs;
        }// getFunctionalityFromCategories

    }// Functionality
}
