﻿using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TaskManager.Client.Models;
using TaskManager.Client.Services;
using TaskManager.Client.Views.AddWindows;
using TaskManager.Client.Views.Components;
using TaskManager.Client.Views.Pages;
using TaskManager.Common.Models;

namespace TaskManager.Client.ViewModels
{
    public class DeskTasksPageViewModel : BindableBase
    {
        private AuthToken _token;
        private DeskModel _desk;
        private UsersRequestService _usersRequestService;
        private TasksRequestService _tasksRequestService;
        private ProjectsRequestService _projectsRequestService;
        private CommonViewService _viewService;

        private DeskTasksPage _page;

        public DelegateCommand OpenNewTaskCommand { get; private set; }
        public DelegateCommand OpenUpdateTaskCommand { get; private set; }
        public DelegateCommand CreateOrUpdateTaskCommand { get; private set; }
        public DelegateCommand DeleteTaskCommand { get; private set; }

        public DeskTasksPageViewModel(AuthToken token, DeskModel desk, DeskTasksPage page)
        {
            _token = token;
            _desk = desk;
            _page = page;

            _viewService = new CommonViewService();
            _usersRequestService = new UsersRequestService();
            _projectsRequestService = new ProjectsRequestService();
            _tasksRequestService = new TasksRequestService();

            TasksByColumns = GetTasksByColumns(_desk.Id);
            _page.TasksGrid.Children.Add(CreateTasksGrid());

            OpenNewTaskCommand = new DelegateCommand(OpenNewTask);
            OpenUpdateTaskCommand = new DelegateCommand(OpenUpdateTask);
            CreateOrUpdateTaskCommand = new DelegateCommand(CreateOrUpdateTask);
            DeleteTaskCommand = new DelegateCommand(DeleteTask);
        }

        #region PROPERTIES
        
        private Dictionary<string, List<TaskClient>> _tasksByColumns = new Dictionary<string, List<TaskClient>>();

        public Dictionary<string, List<TaskClient>> TasksByColumns
        {
            get => _tasksByColumns;
            set 
            { 
                _tasksByColumns = value; 
                RaisePropertyChanged(nameof(TasksByColumns));
            }
        }

        private TaskClient _selectedTask;

        public TaskClient SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                RaisePropertyChanged(nameof(SelectedTask));
            }
        }

        private ClientAction _typeActionWithTask;
        public ClientAction TypeActionWithTask
        {
            get => _typeActionWithTask;
            set
            {
                _typeActionWithTask = value;
                RaisePropertyChanged(nameof(TypeActionWithTask));
            }
        }

        private UserModel _selectedTaskExecutor;
        public UserModel SelectedTaskExecutor
        {
            get => _selectedTaskExecutor;
            set
            {
                _selectedTaskExecutor = value;
                RaisePropertyChanged(nameof(SelectedTaskExecutor));
            }
        }

        private ProjectModel Project
        {
            get => _projectsRequestService.GetProjectById(_token, _desk.ProjectId);
        }
        public List<UserModel> AllProjectUsers
        {
            get => Project.AllUsersIds?.Select(userId => _usersRequestService.GetUserById(_token, userId)).ToList();

        }

        #endregion

        #region METHODS

        private Dictionary<string, List<TaskClient>> GetTasksByColumns(int deskId)
        {
            var tasksByColumns = new Dictionary<string, List<TaskClient>>();
            var allTasks = _tasksRequestService.GetTaskByDesk(_token, deskId);
            foreach (string column in _desk.Columns)
            {
                tasksByColumns.Add(column, allTasks
                    .Where(t => t.Column == column)
                    .Select(t =>
                    {
                        var tV = new TaskClient(t);
                        tV.Creator = _usersRequestService.GetCurrentUser(_token);
                        if (t.ExecutorId != null)
                            tV.Executor = _usersRequestService.GetUserById(_token, (int)t.ExecutorId);                        
                        return tV;
                    }
                    ).ToList());
            }
            return tasksByColumns;
        }

        private void CreateOrUpdateTask()
        {
            if (TypeActionWithTask == ClientAction.Create)
            {
                CreateTask();
            }
            if (TypeActionWithTask == ClientAction.Update)
            {
                UpdateTask();
            }
            UpdatePage();
        }

        private void CreateTask()
        {
            
            SelectedTask.Model.DeskId = _desk.Id;
            SelectedTask.Model.ExecutorId = SelectedTaskExecutor.Id;
            SelectedTask.Model.Column = _desk.Columns.FirstOrDefault();

            var resultAction = _tasksRequestService.CreateTask(_token, SelectedTask.Model);
            _viewService.ShowActionResult(resultAction, "New project is created");
        }

        private void UpdateTask()
        {
            _tasksRequestService.UpdateTask(_token, SelectedTask.Model);
        }

        private void DeleteTask()
        {
            _tasksRequestService.DeleteTask(_token, SelectedTask.Model.Id);

            UpdatePage();
        }

        private void UpdatePage()
        {
            SelectedTask = null;
            TasksByColumns = GetTasksByColumns(_desk.Id);
            _page.TasksGrid.Children.Add(CreateTasksGrid());
            _viewService.CurrentOpenedWindow?.Close();
        }

        private void OpenNewTask()
        {
            TypeActionWithTask = ClientAction.Create;
            SelectedTask = new TaskClient(new TaskModel());
            var wnd = new CreateOrUpdateTaskWindow();
            _viewService.OpenWindow(wnd, this);
        }

        private void OpenUpdateTask()
        {
            TypeActionWithTask = ClientAction.Update;
            var wnd = new CreateOrUpdateTaskWindow();
            _viewService.OpenWindow(wnd, this);
        }

        private Grid CreateTasksGrid()
        {
            ResourceDictionary resource = new ResourceDictionary();
            resource.Source = new Uri("./Resources/Styles/MainStyle.xaml", UriKind.Relative);

            Grid grid = new Grid();
            var row0 = new RowDefinition();
            row0.Height = new GridLength(30);

            var row1 = new RowDefinition();
            
            grid.RowDefinitions.Add(row0);
            grid.RowDefinitions.Add(row1);

            int columnCount = 0;
            foreach (var column in TasksByColumns)
            {
                var col = new ColumnDefinition();
                grid.ColumnDefinitions.Add(col);

                //header
                TextBlock header = new TextBlock();
                header.Text = column.Key;
                header.Style = resource["headerTBlock"] as Style;

                Grid.SetRow(header, 0);
                Grid.SetColumn(header, columnCount);

                grid.Children.Add(header);

                //column
                ItemsControl columnControl = new ItemsControl();
                columnControl.Style = resource["tasksColumnPanel"] as Style;
                Grid.SetRow(columnControl, 1);
                Grid.SetColumn(columnControl, columnCount);

                var taskViews = new List<TaskControl>();

                foreach (var task in column.Value)
                {
                    var taskView = new TaskControl(task);
                    taskViews.Add(taskView);
                }
                columnControl.ItemsSource = taskViews;
                grid.Children.Add(columnControl);

                columnCount++;
            }

            return grid;
        }

        #endregion

    }
}