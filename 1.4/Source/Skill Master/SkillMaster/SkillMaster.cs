using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Ruyu.SkillMaster
{
    [HarmonyPatch(typeof(MainTabWindow_Work))]
    [HarmonyPatch("DoWindowContents")]
    public static class Worktab_Patch
    {
        private static Dictionary<SkillDef, float> maxSkillsCache; // Cache field
        private static float lastCacheUpdateTime; // Last cache update time
        private const float CacheRefreshInterval = 1f; // Cache refresh interval in seconds
        private static readonly List<Pawn> highlightedColonists = new List<Pawn>();
        private static int selectedColonistIndex = -1;
        private static bool isBetterPawnControlModActive = ModChecker.IsBetterPawnControlModActive();
        public static void Postfix(MainTabWindow_Work __instance, Rect rect)
        {
            Rect tableRect = rect;
            DrawMaxSkills(tableRect);
        }
        private static void DrawMaxSkills(Rect rect)
        {
            var maxSkills = GetMaxSkills();
            if (maxSkills == null || maxSkills.Count == 0)
                return;
            // Create solid color textures
            Color fillColor = new Color(0.45f, 0.48f, 0.52f, 0.255f);
            Color backgroundColor = new Color(0f, 0f, 0f, 0f);
            Texture2D fillTexture = SolidColorMaterials.NewSolidColorTexture(fillColor);
            Texture2D backgroundTexture = SolidColorMaterials.NewSolidColorTexture(backgroundColor);

            float lineHeight = Text.LineHeight;
            int maxSkillsPerRow = 7;
            float horizontalPadding = 10f;
            float verticalMargin = 0f;
            float widthAdjustment = 0;
            // Increase window width and height

            int skillsCount = maxSkills.Count;
            int numRows = Mathf.CeilToInt((float)skillsCount / maxSkillsPerRow);

            float rowHeight = lineHeight * numRows;
            float columnWidth = rect.width / maxSkillsPerRow;

            int rowIndex = 0;
            int skillIndex = 0;

            string highestSkillsLabel = "Highest skills";

            if (isBetterPawnControlModActive)
            {
                highestSkillsLabel = "";
                widthAdjustment = columnWidth * 0.5f - 35f - horizontalPadding;
            }

            Rect skillLabelRect = new Rect(rect.x + horizontalPadding , rect.yMax - rowHeight, columnWidth - horizontalPadding - widthAdjustment, lineHeight);

            foreach (KeyValuePair<SkillDef, float> kvp in GetMaxSkills())
            {
                int columnIndex = skillIndex % maxSkillsPerRow;

                if (rowIndex == 0 && columnIndex == 0)
                {
                    // First column in the first row
                    skillLabelRect.x = rect.x + columnWidth * columnIndex + horizontalPadding + widthAdjustment;
                    skillLabelRect.y = rect.yMax - rowHeight + lineHeight * rowIndex;
                    string skillText = $"   {kvp.Key.skillLabel.CapitalizeFirst()} {kvp.Value:0.##}";
                    Widgets.Label(skillLabelRect, highestSkillsLabel);
                    skillIndex++;
                    skillLabelRect.x = rect.x + columnWidth * 1 + horizontalPadding + widthAdjustment;
                    skillLabelRect.y = rect.yMax - rowHeight + lineHeight * rowIndex;
                    if (kvp.Value > 0f)
                    {
                        Rect skillRect = GetSkillRect(rect, 1, rowIndex, maxSkillsPerRow, horizontalPadding, verticalMargin);
                        // Apply the width adjustment to the skillRect
                        skillRect.x += widthAdjustment;
                        skillRect.width = columnWidth - horizontalPadding - widthAdjustment;
                        Widgets.FillableBar(skillRect, kvp.Value / 20f, fillTexture, backgroundTexture, false);
                        List<Pawn> colonistsWithHighestSkill = GetColonistsWithHighestSkill(kvp.Key);
                        if (Mouse.IsOver(skillRect))
                        {
                            Widgets.DrawHighlight(skillRect);
                            string description = GetColonistsDescription(colonistsWithHighestSkill);
                            TooltipHandler.TipRegion(skillRect, description);
                            highlightedColonists.Clear();
                            highlightedColonists.AddRange(colonistsWithHighestSkill);

                            foreach (Pawn col in colonistsWithHighestSkill)
                            {
                                if (Find.CurrentMap.mapPawns.FreeColonistsSpawned != null && Find.CurrentMap.mapPawns.FreeColonistsSpawned.Count != 0)
                                {
                                    TargetHighlighter.Highlight(col, true, true, true);
                                }
                            }
                            if (Event.current.type == EventType.MouseDown)
                            {
                                    JumpToSelectedColonist();
                                    selectedColonistIndex++;
                                    if (selectedColonistIndex >= highlightedColonists.Count)
                                    {
                                        selectedColonistIndex = 0;
                                    }
                            }
                        }
                    }
                    Widgets.Label(skillLabelRect, skillText);
                }
                else if (rowIndex == 1 && columnIndex == 0)
                {
                    string skillText = $"   {kvp.Key.skillLabel.CapitalizeFirst()} {kvp.Value:0.##}";
                    Widgets.Label(skillLabelRect, "");
                    skillIndex++;
                    skillLabelRect.x = rect.x + columnWidth * 1 + horizontalPadding + widthAdjustment;
                    skillLabelRect.y = rect.yMax - rowHeight + lineHeight * rowIndex;
                    // Draw a border around the used skills
                    if (kvp.Value > 0f)
                    {
                        Rect skillRect = GetSkillRect(rect, 1, rowIndex, maxSkillsPerRow, horizontalPadding, verticalMargin);
                        // Apply the width adjustment
                        skillRect.x += widthAdjustment;
                        skillRect.width = columnWidth - horizontalPadding - widthAdjustment;
                        Widgets.FillableBar(skillRect, kvp.Value / 20f, fillTexture, backgroundTexture, false);
                        List<Pawn> colonistsWithHighestSkill = GetColonistsWithHighestSkill(kvp.Key);
                        if (Mouse.IsOver(skillRect))
                        {
                            Widgets.DrawHighlight(skillRect);
                            string description = GetColonistsDescription(colonistsWithHighestSkill);
                            TooltipHandler.TipRegion(skillRect, description);
                            highlightedColonists.Clear();
                            highlightedColonists.AddRange(colonistsWithHighestSkill);

                            foreach (Pawn col in colonistsWithHighestSkill)
                            {
                                if (Find.CurrentMap.mapPawns.FreeColonistsSpawned != null && Find.CurrentMap.mapPawns.FreeColonistsSpawned.Count != 0)
                                {
                                    TargetHighlighter.Highlight(col, true, true, true);
                                }
                            }
                            if (Event.current.type == EventType.MouseDown)
                            {
                                JumpToSelectedColonist();
                                selectedColonistIndex++;
                                if (selectedColonistIndex >= highlightedColonists.Count)
                                {
                                    selectedColonistIndex = 0;
                                }
                            }
                        }
                    }
                    Widgets.Label(skillLabelRect, skillText);
                }
                else
                {
                    skillLabelRect.x = rect.x + columnWidth * columnIndex + horizontalPadding + widthAdjustment;
                    skillLabelRect.y = rect.yMax - rowHeight + lineHeight * rowIndex;

                    string skillText = $"  {kvp.Key.skillLabel.CapitalizeFirst()} {kvp.Value:0.##}";

                    if (kvp.Value > 0f)
                    {
                        Rect skillRect = GetSkillRect(rect, columnIndex, rowIndex, maxSkillsPerRow, horizontalPadding, verticalMargin);

                        // Apply the width adjustment to the skillRect
                        skillRect.x += widthAdjustment;
                        skillRect.width = columnWidth - horizontalPadding - widthAdjustment;

                        Widgets.FillableBar(skillRect, kvp.Value / 20f, fillTexture, backgroundTexture, false);
                        List<Pawn> colonistsWithHighestSkill = GetColonistsWithHighestSkill(kvp.Key);

                        if (Mouse.IsOver(skillRect))
                        {
                            Widgets.DrawHighlight(skillRect);
                            string description = GetColonistsDescription(colonistsWithHighestSkill);
                            TooltipHandler.TipRegion(skillRect, description);
                            highlightedColonists.Clear();
                            highlightedColonists.AddRange(colonistsWithHighestSkill);

                            foreach (Pawn col in colonistsWithHighestSkill)
                            {
                                if (Find.CurrentMap.mapPawns.FreeColonistsSpawned != null && Find.CurrentMap.mapPawns.FreeColonistsSpawned.Count != 0)
                                {
                                    TargetHighlighter.Highlight(col, true, true, true);
                                }
                            }

                            if (Event.current.type == EventType.MouseDown)
                            {
                                JumpToSelectedColonist();
                                selectedColonistIndex++;

                                if (selectedColonistIndex >= highlightedColonists.Count)
                                {
                                    selectedColonistIndex = 0;
                                }
                            }
                        }
                    }
                    Widgets.Label(skillLabelRect, skillText);
                }
                skillIndex++;

                if (columnIndex == maxSkillsPerRow - 1)
                {
                    rowIndex++;
                }
            }
            // Draws a line above the first row with line color
            float lineThickness = 1f; // line thickness
            float lineY = rect.yMax - rowHeight - 5f;
            float lineWidth = rect.width;
            Color lineColor = new Color(1f, 1f, 1f, 0.2f); // Sets the line color
            DrawLine(rect.x - 3f, lineY, lineWidth, lineColor, lineThickness);
        }
        private static void JumpToSelectedColonist()
        {
            if (highlightedColonists.Count > 0 && selectedColonistIndex >= 0 && selectedColonistIndex < highlightedColonists.Count)
            {
                Pawn colonist = highlightedColonists[selectedColonistIndex];
                CameraJumper.TryJumpAndSelect(colonist);
            }
        }
        private static List<Pawn> GetColonistsWithHighestSkill(SkillDef skillDef)
        {
            var maxSkills = GetMaxSkills();

            if (!maxSkills.ContainsKey(skillDef))
            {
                return new List<Pawn>(); // No colonists have the specified skill
            }
            float maxSkillLevel = maxSkills[skillDef];
            return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists
                .Where(pawn => pawn.skills.GetSkill(skillDef).Level == maxSkillLevel)
                .ToList();
        }
        private static string GetColonistsDescription(List<Pawn> colonists)
        {
            string description = "";
            bool jumpStringAppended = false; // Track if "Click to jump to:" string is appended

            foreach (Pawn colonist in colonists)
            {
                string colonistName = colonist.Name.ToStringShort;
                string colonistTitle = colonist.story.Title;
                string colonistGender = colonist.gender.ToString();
                string colonistHealthStatus = colonist.health.summaryHealth.SummaryHealthPercent.ToStringPercent();

                // Append the colonist's description
                if (!jumpStringAppended)
                {
                    description += "Click to jump to:\n\n";
                    jumpStringAppended = true;
                }
                string colonistDescription = $"{colonistName}, {colonistTitle.CapitalizeFirst()} ({colonistGender} colonist)\n";
                description += colonistDescription + "\n";
            }
            return description;
        }
        private static Rect GetSkillRect(Rect rect, int columnIndex, int rowIndex, int maxSkillsPerRow, float horizontalPadding, float verticalMargin)
        {
            float columnWidth = rect.width / maxSkillsPerRow;
            float lineHeight = Text.LineHeight;
            float rowHeight = lineHeight * 2;

            // Calculate the vertical offset based on the row index and vertical margin
            float verticalOffset = lineHeight * rowIndex + rowIndex * verticalMargin;

            return new Rect(
                rect.x + columnWidth * columnIndex + horizontalPadding,
                rect.yMax - rowHeight + verticalOffset,
                columnWidth - horizontalPadding,
                lineHeight
            );
        }
        private static void DrawLine(float x, float y, float length, Color color, float thickness)
        {
            GUI.color = color;
            Rect lineRect = new Rect(x, y + thickness * 0.2f, length, thickness);
            GUI.DrawTexture(lineRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
        }

        private static Dictionary<SkillDef, float> GetMaxSkills()
        {
            var colonists = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists;
            if (colonists == null || colonists.Count() == 0)
                return GetEmptyMaxSkills();

            // Check if cache needs to be refreshed
            if (maxSkillsCache != null && Time.realtimeSinceStartup - lastCacheUpdateTime < CacheRefreshInterval)
                return maxSkillsCache; // Return cached values if available

            maxSkillsCache = new Dictionary<SkillDef, float>();

            foreach (Pawn colonist in colonists)
            {
                foreach (var skill in colonist.skills.skills)
                {
                    if (!maxSkillsCache.ContainsKey(skill.def) || skill.Level > maxSkillsCache[skill.def])
                    {
                        maxSkillsCache[skill.def] = skill.Level;
                    }
                }
            }
            lastCacheUpdateTime = Time.realtimeSinceStartup; // Update the last cache update time
            return maxSkillsCache;
        }
        private static Dictionary<SkillDef, float> GetEmptyMaxSkills()
        {
            var emptyMaxSkills = new Dictionary<SkillDef, float>();
            var skillDefs = DefDatabase<SkillDef>.AllDefsListForReading;

            foreach (var skillDef in skillDefs)
            {
                emptyMaxSkills[skillDef] = 0f;
            }

            return emptyMaxSkills;
        }
    }
    public static class ModChecker
    {
        private static bool isBetterPawnControlModActive;

        static ModChecker()
        {
            isBetterPawnControlModActive = CheckBetterPawnControlMod();
        }

        private static bool CheckBetterPawnControlMod()
        {
            return ModLister.GetActiveModWithIdentifier("voult.betterpawncontrol") != null;
        }

        public static bool IsBetterPawnControlModActive()
        {
            return isBetterPawnControlModActive;
        }
    }
        [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("Ruyu.SkillsOfColony");
            harmony.PatchAll();
        }
    }
}