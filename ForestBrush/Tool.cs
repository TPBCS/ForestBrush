﻿using ColossalFramework.UI;
using ForestBrush.GUI;
using ForestBrush.TranslationFramework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestBrush
{
    public class ForestBrushTool : MonoBehaviour
    {
        private ProbabilityCalculator probabilityCalculator;

        public Brush Brush => UserMod.Settings.SelectedBrush;

        private List<TreeInfo> TreeInfos { get; set; } = new List<TreeInfo>();

        private TreeInfo Container { get; set; } = ForestBrush.Instance.Container;

        public List<Brush> Brushes => UserMod.Settings.Brushes;

        void Awake()
        {
            probabilityCalculator = new ProbabilityCalculator();

            UpdateTool(UserMod.Settings.SelectedBrush.Name);
        }

        public void UpdateTool(string brushName)
        {
            UserMod.Settings.SelectBrush(brushName);

            TreeInfos = new List<TreeInfo>();

            foreach (var tree in Brush.Trees)
            {
                if (!ForestBrush.Instance.Trees.TryGetValue(tree.Name, out TreeInfo treeInfo)) continue;
                if (treeInfo == null) continue;
                TreeInfos.Add(treeInfo);
            }

            Container = CreateBrushPrefab(Brush.Trees);

            ForestBrush.Instance.ForestBrushPanel.LoadBrush(Brush);

            UserMod.SaveSettings();
        }

        private void Add(TreeInfo tree)
        {
            if (TreeInfos.Contains(tree)) return;
            TreeInfos.Add(tree);
            Brush.Add(tree);
        }

        private void Remove(TreeInfo tree)
        {
            if (!TreeInfos.Contains(tree)) return;
            TreeInfos.Remove(tree);
            Brush.Remove(tree);
        }

        public void RemoveAll()
        {
            var infoBuffer = ForestBrush.Instance.ForestBrushPanel.BrushEditSection.TreesList.rowsData.m_buffer;
            var itemBuffer = ForestBrush.Instance.ForestBrushPanel.BrushEditSection.TreesList.rows.m_buffer;
            foreach (TreeInfo tree in infoBuffer)
            {
                Remove(tree);
            }
            foreach (TreeItem item in itemBuffer)
            {
                item?.ToggleCheckbox(false);
            }
        }

        private void AddAll()
        {
            var infoBuffer = ForestBrush.Instance.ForestBrushPanel.BrushEditSection.TreesList.rowsData.m_buffer.Cast<TreeInfo>().ToList();
            TreeInfos = ForestBrush.Instance.Trees.Values.Where(treeInfo => infoBuffer.Contains(treeInfo)).ToList();
            Brush.ReplaceAll(TreeInfos);
            var itemBuffer = ForestBrush.Instance.ForestBrushPanel.BrushEditSection.TreesList.rows.m_buffer;
            for (int i = 0; i < itemBuffer.Length; i++)
            {
                if (i > 99)
                {
                    UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(
                     Translation.Instance.GetTranslation("FOREST-BRUSH-MODAL-LIMITREACHED-TITLE"),
                     Translation.Instance.GetTranslation("FOREST-BRUSH-MODAL-LIMITREACHED-MESSAGE-ALL"),
                     false);
                    return;
                }
                ((TreeItem)itemBuffer[i]).ToggleCheckbox(true);
            }
        }

        public void New(string brushName)
        {
            if (Brushes.Find(b => b.Name == brushName) == null)
            {
                Brush brush = Brush.Default();

                brush.Name = brushName;

                brush.ReplaceAll(TreeInfos);

                Brushes.Add(brush);

                UpdateTool(brushName);
            }
            else Debug.LogError("Error creatin new brush. Brush already exists. This shouldn't happen, please contact the mod author.");
        }

        internal void DeleteCurrent()
        {
            Brushes.Remove(Brush);
            UserMod.Settings.SelectNextBestBrush();
            ForestBrush.Instance.ForestBrushPanel.BrushSelectSection.UpdateDropDown();
            string nextBrush = ForestBrush.Instance.ForestBrushPanel.BrushSelectSection.SelectBrushDropDown.items.Length <= 0 ? Constants.NewBrushName :
                ForestBrush.Instance.ForestBrushPanel.BrushSelectSection.SelectBrushDropDown.selectedValue;
            UpdateTool(nextBrush);
            UserMod.SaveSettings();
        }

        public TreeInfo CreateBrushPrefab(List<Tree> trees)
        {
            var probabilities = probabilityCalculator.Calculate(trees);
            var variations = new TreeInfo.Variation[TreeInfos.Count];
            if (TreeInfos.Count == 0)
            {
                Container.m_variations = variations;
                return Container;
            }
            for (int i = 0; i < TreeInfos.Count; i++)
            {
                var variation = new TreeInfo.Variation();
                variation.m_tree = variation.m_finalTree = TreeInfos[i];
                variation.m_probability = probabilities[i].FloorProbability;
                //TODO not sure if going over the index is a good idea.
                //The calculate method should preserve indizes, but still its kinda risky.
                //alternative to use probabilities.Find(x => x.Name == TreeInfos[i].name); but that is slower
                //another alternative is to return a Dictionary from the Calculate method, but that makes the Calculate method a little more awkward.

                variations[i] = variation;
            }
            Container.m_variations = variations;
            return Container;
        }

        private int GetProbability(int treeIndex)
        {
            float probabilitySum = 0;
            for (int i = 0; i < TreeInfos.Count; i++)
            {
                probabilitySum += Brush.Trees[i].Probability;
            }
            float probability = Brush.Trees[treeIndex].Probability / probabilitySum;
            return Mathf.RoundToInt(probability * 100);
        }

        internal void UpdateTreeList(TreeInfo treeInfo, bool value, bool updateAll)
        {
            if (value) Add(treeInfo);
            else Remove(treeInfo);
            if(updateAll)
            {
                if (value) AddAll();
                else RemoveAll();
            }
            Container = CreateBrushPrefab(Brush.Trees);
            UserMod.SaveSettings();
        }
    }
}