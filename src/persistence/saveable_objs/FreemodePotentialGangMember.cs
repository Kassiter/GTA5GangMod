﻿using GTA.Native;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// basically a potential gang member with more data to be saved
    /// </summary>
    public class FreemodePotentialGangMember : PotentialGangMember
    {
        /// <summary>
        /// male indexes seem to go from 0-20, female from 21-41.
        /// dlc faces seem to be 42 43 44 male and 45 female
        /// </summary>
        private const int FACE_INDEX_COUNT = 46;

        /// <summary>
        /// more drawable data, unused by potential gang members, but should probably be used here
        /// (indexes used are 1 and 5-11)
        /// </summary>
        public int[] extraDrawableIndexes;

        public int[] extraTextureIndexes;

        /// <summary>
        /// props are helmets, masks, glasses etc
        /// </summary>
        public int[] propDrawableIndexes;

        public int[] propTextureIndexes;

        /// <summary>
        /// indexes probably related to makeup and minor facial features, like freckles
        /// </summary>
        public int[] headOverlayIndexes;

        public enum FreemodeGender
        {
            any,
            male,
            female
        }

        public FreemodePotentialGangMember()
        {
            headOverlayIndexes = new int[13];
            extraDrawableIndexes = new int[8];
            extraTextureIndexes = new int[8];
            propDrawableIndexes = new int[3];
            propTextureIndexes = new int[3];
            modelHash = -1;
            myStyle = DressStyle.special;
            linkedColor = MemberColor.white;
            torsoDrawableIndex = -1;
            torsoTextureIndex = -1;
            legsDrawableIndex = -1;
            legsTextureIndex = -1;
            hairDrawableIndex = -1;
            headDrawableIndex = -1;
            headTextureIndex = -1;
        }

        public FreemodePotentialGangMember(Ped targetPed, DressStyle myStyle, MemberColor linkedColor) : base(targetPed, myStyle, linkedColor)
        {
            headOverlayIndexes = new int[13];
            extraDrawableIndexes = new int[8];
            extraTextureIndexes = new int[8];
            propDrawableIndexes = new int[3];
            propTextureIndexes = new int[3];

            //we've already got the model hash, torso indexes and stuff.
            //time to get the new data
            for (int i = 0; i < headOverlayIndexes.Length; i++)
            {
                headOverlayIndexes[i] = Function.Call<int>(Hash._GET_PED_HEAD_OVERLAY_VALUE, targetPed, i);

                if (i < propDrawableIndexes.Length)
                {
                    propDrawableIndexes[i] = Function.Call<int>(Hash.GET_PED_PROP_INDEX, targetPed, i);
                    propTextureIndexes[i] = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, targetPed, i);
                }

                //extra drawable indexes
                if (i == 1)
                {
                    extraDrawableIndexes[0] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, targetPed, i);
                    extraTextureIndexes[0] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, i);
                }

                //indexes from 5 to 11
                if (i > 4 && i < 12)
                {
                    extraDrawableIndexes[i - 4] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, targetPed, i);
                    extraTextureIndexes[i - 4] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, i);
                }
            }

        }

        public override void SetPedAppearance(Ped targetPed)
        {
            int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, targetPed, 1);

            base.SetPedAppearance(targetPed);

            SetPedFaceBlend(targetPed);

            //hair colors seem to go till 64
            int randomHairColor = RandoMath.CachedRandom.Next(0, 64);
            int randomHairStreaksColor = RandoMath.CachedRandom.Next(0, 64);

            Function.Call(Hash._SET_PED_HAIR_COLOR, targetPed, randomHairColor, randomHairStreaksColor);

            //according to what I saw using menyoo, eye colors go from 0 to 32.
            //colors after 23 go pretty crazy, like demon-eyed, so I've decided to stop at 23
            Function.Call(Hash._SET_PED_EYE_COLOR, targetPed, RandoMath.CachedRandom.Next(0, 23));

            //new data time!
            for (int i = 0; i < headOverlayIndexes.Length; i++)
            {
                //indexes for overlays
                Function.Call(Hash.SET_PED_HEAD_OVERLAY, targetPed, i, headOverlayIndexes[i], 1.0f);

                //attempt to keep eyebrow and other colors similar to hair
                //we only mess with beard, eyebrow, blush, lipstick and chest hair colors
                if (i == 1 || i == 2 || i == 5 || i == 8 || i == 10)
                {
                    Function.Call(Hash._SET_PED_HEAD_OVERLAY_COLOR, targetPed, i, 2, randomHairColor, 0);
                }


                if (i < propDrawableIndexes.Length)
                {
                    Function.Call<int>(Hash.SET_PED_PROP_INDEX, targetPed, i, propDrawableIndexes[i], propTextureIndexes[i], true);
                }

                //extra drawable indexes
                if (i == 1)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, i, extraDrawableIndexes[0], extraTextureIndexes[0], pedPalette);
                }

                //indexes from 5 to 11
                if (i > 4 && i < 12)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, i, extraDrawableIndexes[i - 4], extraTextureIndexes[i - 4], pedPalette);
                }
            }

        }

        public static int GetAFaceIndex(FreemodeGender desiredGender)
        {
            int returnedIndex;
            if (desiredGender == FreemodeGender.any)
            {
                returnedIndex = RandoMath.CachedRandom.Next(FACE_INDEX_COUNT);
            }
            else if (desiredGender == FreemodeGender.female)
            {
                returnedIndex = RandoMath.CachedRandom.Next(21, 43);
                if (returnedIndex == 42) returnedIndex = 45;
            }
            else
            {
                returnedIndex = RandoMath.CachedRandom.Next(0, 24);
                if (returnedIndex > 20) returnedIndex += 21;
            }

            return returnedIndex;
        }

        public static void SetPedFaceBlend(Ped targetPed)
        {
            FreemodeGender pedGender = FreemodeGender.any;
            if (targetPed.Model == PedHash.FreemodeMale01)
            {
                pedGender = FreemodeGender.male;
            }
            else if (targetPed.Model == PedHash.FreemodeFemale01)
            {
                pedGender = FreemodeGender.female;
            }
            else
            {
                UI.Notify(string.Concat("attempted face blending for invalid ped type: ", targetPed.Model));
            }

            Function.Call(Hash.SET_PED_HEAD_BLEND_DATA, targetPed, GetAFaceIndex(pedGender), GetAFaceIndex(pedGender), 0, GetAFaceIndex(0),
                GetAFaceIndex(0), 0, 0.5f, 0.5f, 0, false);
        }

        public static FreemodePotentialGangMember FreemodeSimilarEntryCheck(FreemodePotentialGangMember potentialEntry)
        {
            for (int i = 0; i < MemberPool.memberList.Count; i++)
            {
                if (MemberPool.memberList[i].GetType() == typeof(FreemodePotentialGangMember))
                {
                    FreemodePotentialGangMember freeListEntry = MemberPool.memberList[i] as FreemodePotentialGangMember;

                    if (freeListEntry.modelHash == potentialEntry.modelHash &&
                    freeListEntry.hairDrawableIndex == potentialEntry.hairDrawableIndex &&
                    freeListEntry.headDrawableIndex == potentialEntry.headDrawableIndex &&
                    freeListEntry.headTextureIndex == potentialEntry.headTextureIndex &&
                    freeListEntry.legsDrawableIndex == potentialEntry.legsDrawableIndex &&
                    freeListEntry.legsTextureIndex == potentialEntry.legsTextureIndex &&
                    freeListEntry.torsoDrawableIndex == potentialEntry.torsoDrawableIndex &&
                    freeListEntry.torsoTextureIndex == potentialEntry.torsoTextureIndex &&
                    RandoMath.AreIntArrayContentsTheSame(freeListEntry.extraDrawableIndexes, potentialEntry.extraDrawableIndexes) &&
                    RandoMath.AreIntArrayContentsTheSame(freeListEntry.extraTextureIndexes, potentialEntry.extraTextureIndexes) &&
                    RandoMath.AreIntArrayContentsTheSame(freeListEntry.propDrawableIndexes, potentialEntry.propDrawableIndexes) &&
                    RandoMath.AreIntArrayContentsTheSame(freeListEntry.propTextureIndexes, potentialEntry.propTextureIndexes) &&
                    RandoMath.AreIntArrayContentsTheSame(freeListEntry.headOverlayIndexes, potentialEntry.headOverlayIndexes))
                    {
                        return freeListEntry;
                    }
                }
                else continue;

            }
            return null;
        }



    }
}
