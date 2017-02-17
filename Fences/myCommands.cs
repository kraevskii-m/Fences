﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Fences;
using Fences.Properties;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using MessageBox = System.Windows.MessageBox;

[assembly: CommandClass(typeof(MyCommands))]

namespace Fences
{
    public class MyCommands
    {
        private static Database _database;
        private Document _document;
        private int _guessnum = 1;
        private PromptSelectionResult _selAll;
        private SelectionSet _selectionSet;

        [CommandMethod("CreateFenceSetting", CommandFlags.Modal)]
        private static void CreateFenceSetting()
        {
            DialogBox m = new DialogBox();
            m.ShowDialog();
            if (m.DialogResult == DialogResult.OK)
                Settings.Default.path = DialogBox.ReturnValue ? FileCreator.CreateFile() : FileCreator.OpenFile();
            Settings.Default.Save();
        }

        [CommandMethod("CreateFence", CommandFlags.Modal)]
        public void CreateFence()
        {
            if (Settings.Default.path == null)
                CreateFenceSetting();
            _document = Application.DocumentManager.MdiActiveDocument;
            _database = _document.Database;

            Editor editor = _document.Editor;

            editor.WriteMessage("Выберите ось ограждения:");
            _selAll = editor.GetSelection();
            _selectionSet = _selAll.Value;

            MySelect();
        }

        [CommandMethod("CreateFenceGet", CommandFlags.Modal)]
        public void CreateFenceGet()
        {
            FileCreator.GetFromFile(Settings.Default.path);
        }

        private void MySelect()
        {
            if (_selAll.Status == PromptStatus.OK)
                using (Transaction transaction = _document.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in _selectionSet.GetObjectIds())
                        if (id.ObjectClass == RXObject.GetClass(typeof(Polyline)))
                        {
                            GetNumFloor();
                            Polyline pl = (Polyline) transaction.GetObject(id, OpenMode.ForRead);
                            List<Point2d> points = new List<Point2d>();

                            for (int j = 0; j < pl.NumberOfVertices; j++)
                            {
                                Point2d pt = pl.GetPoint2dAt(j);
                                points.Add(pt);
                            }
                            List<List<Point2d>> pointsDimRec = new List<List<Point2d>>();

                            for (int i = 0; i < points.Count - 1; i++)
                            {
                                pointsDimRec.Add(new List<Point2d>());
                                pointsDimRec[i].Add(points[i]);
                                int[] segments = Divide((int) points[i].GetDistanceTo(points[i + 1]), i,
                                    points.Count - 1);
                                int dist = 0;
                                for (int k = 0; k < segments.Length - 1; k++)
                                {
                                    dist += segments[k];
                                    Drawer(points[i], points[i + 1], dist);
                                    pointsDimRec[i].Add(MoveDist(points[i], points[i + 1], dist));
                                }
                                pointsDimRec[i].Add(points[i + 1]);
                                FileCreator.ToFile(id.ToString(), points[i].GetDistanceTo(points[i + 1]),
                                    segments.Length - 1, Settings.Default.path, _guessnum);
                            }
                            ChangeLayer(transaction, CreateLayer("КМ-РАЗМ", Color.FromColorIndex(ColorMethod.ByAci, 1),
                                LineWeight.LineWeight020));
                            for (int i = 0; i <= pointsDimRec.Count - 1; i++)
                            for (int j = 0; j <= pointsDimRec[i].Count - 2; j++)
                                Dimension.Dim(pointsDimRec[i][j], pointsDimRec[i][j + 1]); //TODO Не работает на "большом" листе
                        }
                        else
                        {
                            MessageBox.Show("Используйте только полилинии");
                        }
                    transaction.Commit();
                }
        }

        private void GetNumFloor()
        {
            _document.Editor.WriteMessage("На скольких этажах встречается({0}):", _guessnum);
            //TODO Работает не так как задумывалось
            PromptIntegerOptions pKeyOpts = new PromptIntegerOptions("");

            PromptIntegerResult pKeyRes = _document.Editor.GetInteger(pKeyOpts);

            if (pKeyRes.Value != _guessnum || pKeyRes.Value >= 0)
                _guessnum = pKeyRes.Value;
        }

        public static int[] Divide(int lenght, int index, int n)
        {
            if (lenght < 200)
                throw new ArgumentException("Такой длины не бывает: " + lenght);
            int firstLen = 150;
            int lastLen = 150;

            if (index == 0)
                firstLen = 100;
            if (index == n - 1)
                lastLen = 100;

            if (lenght < firstLen + 190 + lastLen)
            {
                if (index == 0)
                    return new[] {firstLen, lenght - firstLen};
                return new[] {lenght - lastLen, lastLen};
            }
            int middleLen = lenght - firstLen - lastLen;
            int numSeg = middleLen % 900 == 0 ? middleLen / 900 : middleLen / 900 + 1;
            int minSegLenght = middleLen / numSeg / 10 * 10;
            int rest = middleLen - numSeg * minSegLenght;
            int[] result = new int[numSeg + 2];
            result[0] = firstLen;
            result[result.Length - 1] = lastLen;
            for (int i = 1; i < result.Length - 1; i++)
            {
                result[i] = minSegLenght;
                int curRest = Math.Min(rest, 10);
                result[i] += curRest;
                rest -= curRest;
            }
            return result;
        }

        private static Point2d MoveDist(Point2d p1, Point2d p2, double dist)
        {
            Vector2d p12 = p1.GetVectorTo(p2);
            return p1.Add(p12.GetNormal().MultiplyBy(dist));
        }

        private void Drawer(Point2d p1, Point2d p2, double dist)
        {
            DrawBar(MoveDist(p1, p2, dist), p1.GetVectorTo(p2).Angle);
        }

        private void DrawBar(Point2d p, double ang)
        {
            using (Transaction transaction = _document.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = transaction.GetObject(_database.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTblRec =
                    transaction.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                ChangeLayer(transaction,
                    CreateLayer("Опорная плита стойки", Color.FromColorIndex(ColorMethod.ByAci, 50),
                        LineWeight.LineWeight018));

                const double w = 180;
                const double h = 120;

                Polyline bar = new Polyline();
                bar.AddVertexAt(0, p.Add(new Vector2d(w / 2, h / 2)), 0, 0, 0);
                bar.AddVertexAt(0, p.Add(new Vector2d(-w / 2, h / 2)), 0, 0, 0);
                bar.AddVertexAt(0, p.Add(new Vector2d(-w / 2, -h / 2)), 0, 0, 0);
                bar.AddVertexAt(0, p.Add(new Vector2d(w / 2, -h / 2)), 0, 0, 0);
                bar.AddVertexAt(0, p.Add(new Vector2d(w / 2, h / 2)), 0, 0, 0);

                bar.Closed = true;


                Matrix3d curUcsMatrix = _document.Editor.CurrentUserCoordinateSystem;
                CoordinateSystem3d curUcs = curUcsMatrix.CoordinateSystem3d;

                bar.TransformBy(Matrix3d.Rotation(ang, curUcs.Zaxis, new Point3d(p.X, p.Y, 0)));

                if (acBlkTblRec != null)
                {
                    acBlkTblRec.AppendEntity(bar);
                    transaction.AddNewlyCreatedDBObject(bar, true);
                }

                ChangeLayer(transaction,
                    CreateLayer("Стойки ограждений", Color.FromColorIndex(ColorMethod.ByAci, 70),
                        LineWeight.LineWeight040));

                const double wr = 32;
                const double hr = 20.8;
                const double rad = 0.414213562373095;

                Polyline rack = new Polyline();
                rack.AddVertexAt(0, p.Add(new Vector2d(-wr / 2, hr / 2)), 0, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(-hr / 2, wr / 2)), rad, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(hr / 2, wr / 2)), 0, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(wr / 2, hr / 2)), rad, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(wr / 2, -hr / 2)), 0, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(hr / 2, -wr / 2)), rad, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(-hr / 2, -wr / 2)), 0, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(-wr / 2, -hr / 2)), rad, 0, 0);
                rack.AddVertexAt(0, p.Add(new Vector2d(-wr / 2, hr / 2)), 0, 0, 0);

                rack.Closed = true;

                rack.TransformBy(Matrix3d.Rotation(ang, curUcs.Zaxis, new Point3d(p.X, p.Y, 0)));

                if (acBlkTblRec != null)
                {
                    acBlkTblRec.AppendEntity(rack);
                    transaction.AddNewlyCreatedDBObject(rack, true);

                    DBObjectCollection acDbObjColl = rack.GetOffsetCurves(4);

                    foreach (Entity acEnt in acDbObjColl)
                    {
                        acBlkTblRec.AppendEntity(acEnt);
                        transaction.AddNewlyCreatedDBObject(acEnt, true);
                    }
                }
                transaction.Commit();
            }
        }

        private static LayerTableRecord CreateLayer(string name, Color color, LineWeight weight)
        {
            LayerTableRecord layer = new LayerTableRecord
            {
                Name = name,
                Color = color,
                LineWeight = weight
            };
            return layer;
        }

        private static void ChangeLayer(Transaction acTrans, LayerTableRecord ltr)
            //Переносим стойки и пластины на нужный слой
        {
            LayerTable lt = (LayerTable) acTrans.GetObject(_database.LayerTableId, OpenMode.ForRead);
            if (lt.Has(ltr.Name))
            {
                _database.Clayer = lt[ltr.Name];
            }
            else
            {
                lt.UpgradeOpen();
                ObjectId ltId = lt.Add(ltr);
                acTrans.AddNewlyCreatedDBObject(ltr, true);
                _database.Clayer = ltId;
            }
        }
    }
}