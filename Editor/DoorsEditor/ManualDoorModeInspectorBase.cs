﻿using System;
using Edgar.GraphBasedGenerator.Grid2D.Exceptions;
using UnityEditor;
using UnityEngine;

namespace Edgar.Unity.Editor
{
    public abstract class ManualDoorModeInspectorBase : IDoorModeInspector
    {
        private Mode currentMode;
        private Vector3Int firstPoint;
        private bool hasFirstTile;
        private bool hasSecondTile;

        // ReSharper disable once InconsistentNaming
        protected readonly SerializedObject serializedObject;
        // ReSharper disable once InconsistentNaming
        protected readonly SerializedProperty serializedProperty;
        // ReSharper disable once InconsistentNaming
        protected readonly Doors doors;


        protected ManualDoorModeInspectorBase(SerializedObject serializedObject, Doors doors, SerializedProperty serializedProperty)
        {
            this.serializedObject = serializedObject;
            this.doors = doors;
            this.serializedProperty = serializedProperty;
        }

        public void OnInspectorGUI()
        {
            var addDoorsNew = GUILayout.Toggle(currentMode == Mode.AddDoors, "Add door positions", GUI.skin.button);
            var deleteDoorsNew = GUILayout.Toggle(currentMode == Mode.DeleteDoors, "Delete door positions", GUI.skin.button);

            if (addDoorsNew && currentMode != Mode.AddDoors)
            {
                currentMode = Mode.AddDoors;
            }
            else if (deleteDoorsNew && currentMode != Mode.DeleteDoors)
            {
                currentMode = Mode.DeleteDoors;
            }
            else if (!addDoorsNew && !deleteDoorsNew)
            {
                currentMode = Mode.Idle;
            }

            if (GUILayout.Button("Delete all door positions"))
            {
                DeleteAllDoors();
            }

            try
            {
                if (doors.SelectedMode == Doors.DoorMode.Manual)
                {
                    var polygon = RoomTemplatesLoader.GetPolygonFromRoomTemplate(doors.gameObject);
                    var doorPositions = doors.GetDoorMode().GetDoors(polygon);

                    if (doorPositions.Count != doors.ManualDoorModeData.DoorsList.Count)
                    {
                        EditorGUILayout.HelpBox(
                            "There seems to be a door of length 1 that is at the corner of the outline, which is currently not supported. Either use outline override to change the outline or remove the door position.",
                            MessageType.Error);
                    }
                }
            }
            catch (DoorModeException)
            {

            }

            ShowAdditionalFields();
        }

        protected abstract void DeleteAllDoors();

        protected virtual void ShowAdditionalFields()
        {

        }

        public void OnSceneGUI()
        {
            DrawAllDoors();

            switch (currentMode)
            {
                case Mode.AddDoors:
                    HandleAddDoors();
                    break;

                case Mode.DeleteDoors:
                    HandleDeleteDoors();
                    break;
            }
        }

        protected abstract void DrawAllDoors();

        private void HandleDeleteDoors()
        {
            var gameObject = doors.transform.gameObject;
            var e = Event.current;

            var tilePosition = GetCurrentTilePosition();

            // Make sure that the current active object in the inspector is not deselected
            Selection.activeGameObject = gameObject;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            if (e.type == EventType.MouseUp)
            {
                RemoveDoor(tilePosition);

                Event.current.Use();
            }

            // Mouse down must be also used, otherwise there were some bugs after removing doors
            if (e.type == EventType.MouseDown)
            {
                Event.current.Use();
            }
        }

        protected abstract void RemoveDoor(Vector3Int position);

        private void HandleAddDoors()
        {
            var gameObject = doors.transform.gameObject;
            var e = Event.current;

            // Make sure that the current active object in the inspector is not deselected
            Selection.activeGameObject = gameObject;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            // Compute tile position below the mouse cursor
            var tilePosition = GetCurrentTilePosition();

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        firstPoint = tilePosition;
                        hasFirstTile = true;
                        hasSecondTile = false;
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        if (hasFirstTile)
                        {
                            hasSecondTile = true;
                        }
                        e.Use();
                    }

                    break;
            }

            // If we have the first tile, we can show how would the door look like if we released the mouse button
            if (hasFirstTile)
            {
                var from = firstPoint;
                var to = tilePosition;

                if (from.x != to.x && from.y != to.y)
                {
                    to.x = from.x;
                }

                DrawPreview(from, to);

                // If we also have the second tile, we can complete the door
                if (hasSecondTile)
                {
                    hasFirstTile = false;
                    hasSecondTile = false;

                    AddDoor(from, to);
                }

                SceneView.RepaintAll();
            }
        }

        protected abstract void DrawPreview(Vector3Int from, Vector3Int to);

        protected abstract void AddDoor(Vector3Int from, Vector3Int to);

        private Vector3Int GetCurrentTilePosition()
        {
            var gameObject = doors.transform.gameObject;
            var grid = gameObject.GetComponentInChildren<Grid>();

            var mousePosition = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
            mousePosition.z = 0;
            var tilePosition = grid.WorldToCell(mousePosition);
            tilePosition.z = 0;

            return tilePosition;
        }

        protected enum Mode
        {
            Idle, AddDoors, DeleteDoors
        }
    }
}