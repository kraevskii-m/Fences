﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Fences.Properties;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Fences
{
    public class TableCreator
    {
        private Document _document;
        private Editor _editor;
        private readonly FencesAcad _fencesAcad = new RealFencesAcad(Application.DocumentManager.MdiActiveDocument);
        private readonly FileDatabase _fileDatabase = new FileDatabase();

        public void GetDataFromSelection() //TODO Finish
        {
            _document = Application.DocumentManager.MdiActiveDocument;
            _editor = _document.Editor;
            _editor.WriteMessage("Выделите секцию/секции, для которых нужно создать таблицу:");

            ISet<ObjectId> ids = _fencesAcad.GetSelectedFenceIds();

            //TODO Need to make first layer current

            List<Polyline> list = new List<Polyline>();

            foreach (Polyline pl in _fencesAcad.GetFences(ids))
                if (pl.Layer == "КМ-ОСН") //TODO: Bad solution
                    list.Add(pl);
            _fileDatabase.GetTotalNumbers(list);
            Calculator(Settings.Default.CounterLength, Settings.Default.CounterPils);

            CreateTable(Settings.Default.total60X30X4, Settings.Default.total40X4,
                Settings.Default.totalT10,
                Settings.Default.totalT4, Settings.Default.totalT14);
        }

        private void Calculator(double lng, double pls)
        {
            //lng = lng * 0.001;
            int brs = (int)Math.Ceiling(lng * 0.001 / 0.100 - pls);
            double total60X30X4 = Math.Ceiling(lng * Settings.Default.top * 0.001);
            double total40X4 = pls * Settings.Default.pil * Settings.Default.pilLength +
                               (lng * 0.001 - 0.04 * pls) * Settings.Default.pil;
            double totalT10 = pls * Settings.Default.btm;
            double totalT14 = brs * Settings.Default.barLength * Settings.Default.bar;

            Settings.Default.total60X30X4 += Math.Ceiling(total60X30X4) * 0.001;
            Settings.Default.total40X4 += Math.Ceiling(total40X4) * 0.001;
            Settings.Default.totalT10 += Math.Ceiling(totalT10) * 0.001;

            Settings.Default.totalT14 += Math.Ceiling(totalT14) * 0.001;
        }

        private void CreateTable(double t60, double t40, double t10, double t4, double t14)
        {
            Settings.Default.totalT4 += Math.Ceiling(Settings.Default.NumEnd * Settings.Default.ending) * 0.001;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptPointResult pr = ed.GetPoint("\nУкажите место расположения таблицы :");
            Table tb = new Table();
            //EditTablestyle(doc);
            tb.TableStyle = db.Tablestyle;
            tb.SetSize(14, 6);
            tb.SetRowHeight(3);
            tb.SetColumnWidth(14);
            tb.Position = pr.Value;

            string[,] str = new string[14, 7];
            for (int i = 0; i < tb.Rows.Count; i++)
            for (int j = 0; j < tb.Columns.Count; j++)
                str[i, j] = "-";

            str[0, 0] = "Техническая спецификация стали";
            str[1, 0] = "Вид профиля ГОСТ";
            str[1, 1] = "Марка металла ГОСТ";
            str[1, 2] = "Обозначение профиля, мм";
            str[1, 3] = "№ п/п";
            str[1, 4] = "Ограждение";
            str[1, 5] = "Общая масса, т";
            str[2, 0] = "Трубы стальные прямоугольные по ГОСТ 8645-68";
            str[2, 1] = "Cт3пс ГОСТ 380-2005";
            str[2, 2] = "Гн □ 60х30х4";
            str[2, 4] = t60.ToString(CultureInfo.CurrentCulture);
            str[2, 5] = t60.ToString(CultureInfo.CurrentCulture);
            str[3, 4] = t60.ToString(CultureInfo.CurrentCulture);
            str[3, 5] = t60.ToString(CultureInfo.CurrentCulture);
            str[3, 0] = "Всего профиля";
            str[4, 0] = "Стальные гнутые замкнутые сварные квадратные профили по ГОСТ 30245 - 2003";
            str[4, 1] = "Cт3пс ГОСТ 380-2005";
            str[4, 2] = "Гн □ 40х4";
            str[4, 4] = t40.ToString(CultureInfo.CurrentCulture);
            str[4, 5] = t40.ToString(CultureInfo.CurrentCulture);
            str[5, 4] = t40.ToString(CultureInfo.CurrentCulture);
            str[5, 5] = t40.ToString(CultureInfo.CurrentCulture);
            str[5, 0] = "Всего профиля";
            str[6, 0] = "Прокат листовой Горячекатаный ГОСТ 19903-74*";
            str[6, 1] = "C245 ГОСТ 27772-2015";
            str[6, 2] = "t 10";
            str[6, 4] = t10.ToString(CultureInfo.CurrentCulture);
            str[6, 5] = t10.ToString(CultureInfo.CurrentCulture);
            str[7, 2] = "t 4";
            str[7, 4] = t4.ToString(CultureInfo.CurrentCulture);
            str[7, 5] = t4.ToString(CultureInfo.CurrentCulture);
            str[8, 4] = (t4 + t10).ToString(CultureInfo.CurrentCulture);
            str[8, 5] = (t4 + t10).ToString(CultureInfo.CurrentCulture);
            str[8, 0] = "Всего профиля";
            str[9, 0] = "Прокат стальной горячекатаный квадратный ГОСТ 2591-2006";
            str[9, 1] = "Cт3пс ГОСТ 380-2005";
            str[9, 2] = "■ 14";
            str[9, 4] = t14.ToString(CultureInfo.CurrentCulture);
            str[9, 5] = t14.ToString(CultureInfo.CurrentCulture);
            str[10, 4] = t14.ToString(CultureInfo.CurrentCulture);
            str[10, 5] = t14.ToString(CultureInfo.CurrentCulture);
            str[10, 0] = "Всего профиля";
            str[11, 4] = (t14 + t4 + t10 + t40 + t60).ToString(CultureInfo.CurrentCulture);
            str[11, 5] = (t14 + t4 + t10 + t40 + t60).ToString(CultureInfo.CurrentCulture);
            str[11, 0] = "Всего масса материала по обьекту";
            str[12, 0] = "В том числе по маркам стали";
            str[12, 2] = "Ст3пс";
            str[12, 4] = (t14 + t40 + t60).ToString(CultureInfo.CurrentCulture);
            str[12, 5] = (t14 + t40 + t60).ToString(CultureInfo.CurrentCulture);
            str[13, 2] = "С245";
            str[13, 4] = (t4 + t10).ToString(CultureInfo.CurrentCulture);
            str[13, 5] = (t4 + t10).ToString(CultureInfo.CurrentCulture);

            for (int i = 0; i < 12; i++)
                str[i + 2, 3] = (i + 1).ToString();

            CellRange mcells1 = CellRange.Create(tb, 3, 0, 3, 2); //TODO Probably, not the best solution
            tb.MergeCells(mcells1);
            CellRange mcells2 = CellRange.Create(tb, 5, 0, 5, 2);
            tb.MergeCells(mcells2);
            CellRange mcells3 = CellRange.Create(tb, 8, 0, 8, 2);
            tb.MergeCells(mcells3);
            CellRange mcells4 = CellRange.Create(tb, 10, 0, 10, 2);
            tb.MergeCells(mcells4);
            CellRange mcells5 = CellRange.Create(tb, 11, 0, 11, 2);
            tb.MergeCells(mcells5);
            CellRange mcells6 = CellRange.Create(tb, 11, 0, 11, 2);
            tb.MergeCells(mcells6);
            CellRange mcells7 = CellRange.Create(tb, 12, 0, 13, 1);
            tb.MergeCells(mcells7);
            CellRange mcells8 = CellRange.Create(tb, 6, 0, 7, 0);
            tb.MergeCells(mcells8);
            CellRange mcells9 = CellRange.Create(tb, 6, 1, 7, 1);
            tb.MergeCells(mcells9);

            for (int i = 0; i < tb.Rows.Count; i++)
            for (int j = 0; j < tb.Columns.Count; j++)
            {
                tb.Cells[i, j].TextHeight = 1;
                tb.Cells[i, j].TextString = str[i, j];
                tb.Cells[i, j].Alignment = CellAlignment.MiddleCenter;
            }
            tb.GenerateLayout();

            Transaction tr =
                doc.TransactionManager.StartTransaction();
            using (tr)
            {
                Layer.ChangeLayer(tr, Layer.CreateLayer("ТАБЛИЦА", Color.FromColorIndex(ColorMethod.ByAci, 20),
                    LineWeight.LineWeight030), db);
                BlockTable bt =
                    (BlockTable) tr.GetObject(
                        doc.Database.BlockTableId,
                        OpenMode.ForRead
                    );
                BlockTableRecord btr =
                    (BlockTableRecord) tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite
                    );
                btr.AppendEntity(tb);
                tr.AddNewlyCreatedDBObject(tb, true);
                tr.Commit();
            }

            Settings.Default.total60X30X4 = 0;
            Settings.Default.total40X4 = 0;
            Settings.Default.totalT10 = 0;
            Settings.Default.totalT4 = 0;
            Settings.Default.totalT14 = 0;
        }
        /*
        private static void EditTablestyle(Document doc)
        {
            Database db = doc.Database;

            Transaction tr =
                doc.TransactionManager.StartTransaction();
            using (tr)
            {
                const string styleName = "Расчет ограждений";

                DBDictionary sd =
                    (DBDictionary) tr.GetObject(
                        db.TableStyleDictionaryId,
                        OpenMode.ForRead
                    );

                if (sd.Contains(styleName))
                {
                    sd.GetAt(styleName);
                }
                else
                {
                    TableStyle ts = new TableStyle();
                    ts.SetAlignment(CellAlignment.MiddleCenter,
                        (int) (RowType.TitleRow |
                               RowType.HeaderRow |
                               RowType.DataRow));

                    ts.PostTableStyleToDatabase(db, styleName);
                    tr.AddNewlyCreatedDBObject(ts, true);
                    tr.Commit();
                    db.Tablestyle = ts.ObjectId;
                }
            }
        } */
    }
}