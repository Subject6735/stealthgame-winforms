using System;
using System.Drawing;
using System.Windows.Forms;
using StealthGame.Model;
using StealthGame.Persistence;

namespace StealthGame.View
{
    /// <summary>
    /// StealthGame Form, handles the graphical elements.
    /// </summary>
    public partial class StealthGameForm : Form
    {
        #region Fields

        /// <summary>
        /// Data access.
        /// </summary>
        private IStealthGameDataAccess _dataAccess;

        /// <summary>
        /// Model access.
        /// </summary>
        private StealthGameModel _model;

        /// <summary>
        /// The current game table.
        /// </summary>
        private Button[,] _buttonGrid;

        /// <summary>
        /// Game tables.
        /// </summary>
        private Button[,] _buttonGridEasy;
        private Button[,] _buttonGridMedium;
        private Button[,] _buttonGridHard;

        /// <summary>
        /// Default game tables.
        /// </summary>
        private Button[,] _buttonGridEasyDef;
        private Button[,] _buttonGridMediumDef;
        private Button[,] _buttonGridHardDef;

        /// <summary>
        /// Determines whether the player reached the exit.
        /// </summary>
        private bool _exitReached;

        /// <summary>
        /// Determines whether the player is detected.
        /// </summary>
        private bool _detected;

        /// <summary>
        /// The timer, used for moving guards.
        /// </summary>
        private Timer _timer;

        #endregion
        
        #region Constructor

        /// <summary>
        /// StealthGameForm constructor.
        /// </summary>
        public StealthGameForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Form event handlers

        /// <summary>
        /// Sets up the game on load.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StealthGame_Load(object sender, EventArgs e)
        {
            _dataAccess = new StealthGameFileDataAccess();
            _model = new StealthGameModel(_dataAccess);

            _model.NewGame();

            GenerateTables();
            Controls.Add(EasyTablePanel);
            Controls.Remove(MediumTablePanel);
            Controls.Remove(HardTablePanel);

            _resumeGameOption.Enabled = false;

            _buttonGridEasyDef = _buttonGridEasy;
            _buttonGridMediumDef = _buttonGridMedium;
            _buttonGridHardDef = _buttonGridHard;

            _exitReached = false;
            _buttonGrid = _buttonGridEasy;

            SetupTable();
            SetupMenus();

            _timer = new Timer
            {
                Interval = 1000
            };

            _timer.Tick += new EventHandler(Timer_Tick);

            _model.PlayerDetected += new EventHandler<StealthGameEventArgs>(Game_PlayerDetected);
            _model.PlayerReachedExit += new EventHandler<StealthGameEventArgs>(Game_PlayerReachedExit);

            _timer.Start();
        }

        #endregion

        #region Menu event handlers

        /// <summary>
        /// New game option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGameOption_Click(object sender, EventArgs e)
        {
            _saveGameOption.Enabled = true;

            _model.NewGame();

            _exitReached = false;

            if (_model.GameDifficulty == GameDifficulty.Easy)
            {
                Controls.Add(EasyTablePanel);
                Controls.Remove(MediumTablePanel);
                Controls.Remove(HardTablePanel);
                _buttonGrid = _buttonGridEasy;
            }

            if (_model.GameDifficulty == GameDifficulty.Medium)
            {
                Controls.Remove(EasyTablePanel);
                Controls.Add(MediumTablePanel);
                Controls.Remove(HardTablePanel);
                _buttonGrid = _buttonGridMedium;
            }

            if (_model.GameDifficulty == GameDifficulty.Hard)
            {
                Controls.Remove(EasyTablePanel);
                Controls.Remove(MediumTablePanel);
                Controls.Add(HardTablePanel);
                _buttonGrid = _buttonGridHard;
            }

            _pauseGameOption.Enabled = true;
            _resumeGameOption.Enabled = false;

            KeyDown -= new KeyEventHandler(StealthGameForm_KeyDown);
            KeyDown += new KeyEventHandler(StealthGameForm_KeyDown);

            SetupTable();
            SetupMenus();

            _timer.Start();
        }

        /// <summary>
        /// Load game option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadGameOption_Click(object sender, EventArgs e)
        {
            bool restartTimer = _timer.Enabled;
            _timer.Stop();

            if (_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Load game
                    await _model.LoadGameAsync(_openFileDialog.FileName);

                    if (_model.Table.TableSize == _model.Table.EasySize)
                    {
                        Controls.Add(EasyTablePanel);
                        Controls.Remove(MediumTablePanel);
                        Controls.Remove(HardTablePanel);
                        _buttonGrid = _buttonGridEasy;
                    }

                    if (_model.Table.TableSize == _model.Table.MediumSize)
                    {
                        Controls.Remove(EasyTablePanel);
                        Controls.Add(MediumTablePanel);
                        Controls.Remove(HardTablePanel);
                        _buttonGrid = _buttonGridMedium;
                    }

                    if (_model.Table.TableSize == _model.Table.HardSize)
                    {
                        Controls.Remove(EasyTablePanel);
                        Controls.Remove(MediumTablePanel);
                        Controls.Add(HardTablePanel);
                        _buttonGrid = _buttonGridHard;
                    }

                    _saveGameOption.Enabled = true;
                }
                catch (StealthGameDataException)
                {
                    MessageBox.Show("Loading game failed!" + Environment.NewLine + "Invalid file path or format.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    _model.NewGame();
                    _saveGameOption.Enabled = true;
                }

                SetupTable();
            }

            if (restartTimer)
                _timer.Start();
        }

        /// <summary>
        /// Save game option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveGameOption_Click(object sender, EventArgs e)
        {
            bool restartTimer = _timer.Enabled;
            _timer.Stop();

            if (_saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Stop guards, pause game
                    // Some methods

                    // Save game
                    await _model.SaveGameAsync(_saveFileDialog.FileName);
                }
                catch (StealthGameDataException)
                {
                    MessageBox.Show("Saving game failed!" + Environment.NewLine + "Invalid file path or folder is readonly.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (restartTimer)
                _timer.Start();
        }

        /// <summary>
        /// Pause game option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseGameOption_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            _pauseGameOption.Enabled = false;
            _resumeGameOption.Enabled = true;
            KeyDown -= new KeyEventHandler(StealthGameForm_KeyDown);
        }

        /// <summary>
        /// Resume game event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResumeGameOption_Click(object sender, EventArgs e)
        {
            _timer.Start();
            _pauseGameOption.Enabled = true;
            _resumeGameOption.Enabled = false;
            KeyDown += new KeyEventHandler(StealthGameForm_KeyDown);
        }

        /// <summary>
        /// Quit option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QuitGameOption_Click(object sender, EventArgs e)
        {
            bool restartTimer = _timer.Enabled;
            _timer.Stop();

            if (MessageBox.Show("Are you sure you want to quit?", "Stealth Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Close();
            }
            else
            {
                if (restartTimer)
                    _timer.Start();
            }
        }

        /// <summary>
        /// Rules option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpOption_Click(object sender, EventArgs e)
        {
            bool restartTimer = _timer.Enabled;
            _timer.Stop();

            string help =
                "You are the green dot."
                + Environment.NewLine + "You have to reach the exit (indicated by a green area) to win the game." 
                + Environment.NewLine + "Avoid being spotted by guards (red dots), they have a vision cone, indicated by blue areas."
                + Environment.NewLine + "To move, use W, A, S, D; you can only move vertically and horizontially."
                + Environment.NewLine + "You can change the difficulty by selecting the difficulty option then starting a new game."
                + Environment.NewLine + "To pause the game, select pause, to resume, select resume."
                + Environment.NewLine + "To start a new game, load a game, save the game or quit, select options.";

            if (MessageBox.Show(help, "Stealth Game", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
            {
                if (restartTimer)
                    _timer.Start();
            }
        }

        /// <summary>
        /// Easy option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EasyOption_Click(object sender, EventArgs e)
        {
            // Set checked state in options
            _easyOption.Checked = true;
            _mediumOption.Checked = false;
            _hardOption.Checked = false;

            // Set default table
            _buttonGridEasy = _buttonGridEasyDef;

            // Set difficulty
            _model.GameDifficulty = GameDifficulty.Easy;
        }

        /// <summary>
        /// Medium option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MediumOption_Click(object sender, EventArgs e)
        {
            // Set checked state in options
            _easyOption.Checked = false;
            _mediumOption.Checked = true;
            _hardOption.Checked = false;

            // Set default table
            _buttonGridMedium = _buttonGridMediumDef;

            // Set difficulty
            _model.GameDifficulty = GameDifficulty.Medium;
        }

        /// <summary>
        /// Hard option event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HardOption_Click(object sender, EventArgs e)
        {
            // Set checked state in options
            _easyOption.Checked = false;
            _mediumOption.Checked = false;
            _hardOption.Checked = true;

            // Set default table
            _buttonGridHard = _buttonGridHardDef;

            // Set difficulty
            _model.GameDifficulty = GameDifficulty.Hard;
        }

        #endregion

        #region Game event handlers

        /// <summary>
        /// Moves guards every second, refreshing vision cones and checking if the player is detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            _model.MoveGuards();
            RefreshVisionCone();
            RefreshGuards();
            _model.GuardDetect();
        }

        /// <summary>
        /// Handles the event when the player is detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Game_PlayerDetected(object sender, StealthGameEventArgs e)
        {
            if (e.IsOver)
            {
                _timer.Stop();
                KeyDown -= new KeyEventHandler(StealthGameForm_KeyDown);
                _saveGameOption.Enabled = false;

                var msgbox = MessageBox.Show("Game over. You have been detected!" + Environment.NewLine + "Start new game (Yes) or quit (No)?", "Stealth Game", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                if (msgbox == DialogResult.Yes)
                {
                    _saveGameOption.Enabled = true;

                    _model.NewGame();

                    _exitReached = false;

                    if (_model.GameDifficulty == GameDifficulty.Easy)
                    {
                        Controls.Add(EasyTablePanel);
                        Controls.Remove(MediumTablePanel);
                        Controls.Remove(HardTablePanel);
                        _buttonGrid = _buttonGridEasy;
                    }

                    if (_model.GameDifficulty == GameDifficulty.Medium)
                    {
                        Controls.Remove(EasyTablePanel);
                        Controls.Add(MediumTablePanel);
                        Controls.Remove(HardTablePanel);
                        _buttonGrid = _buttonGridMedium;
                    }

                    if (_model.GameDifficulty == GameDifficulty.Hard)
                    {
                        Controls.Remove(EasyTablePanel);
                        Controls.Remove(MediumTablePanel);
                        Controls.Add(HardTablePanel);
                        _buttonGrid = _buttonGridHard;
                    }

                    _pauseGameOption.Enabled = true;
                    _resumeGameOption.Enabled = false;

                    KeyDown -= new KeyEventHandler(StealthGameForm_KeyDown);
                    KeyDown += new KeyEventHandler(StealthGameForm_KeyDown);

                    SetupTable();
                    SetupMenus();

                    _timer.Start();
                }
                else
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Handles the event when the player reached the exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Game_PlayerReachedExit(object sender, StealthGameEventArgs e)
        {
            if (e.IsOver)
            {
                _timer.Stop();
                KeyDown -= new KeyEventHandler(StealthGameForm_KeyDown);
                _saveGameOption.Enabled = false;

                var msgbox = MessageBox.Show("Congratulations, you won! You reached the exit." + Environment.NewLine + "Start new game (Yes) or quit (No)?", "Stealth Game", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (msgbox == DialogResult.Yes)
                {
                    _saveGameOption.Enabled = true;

                    _model.NewGame();

                    _exitReached = false;

                    if (_model.GameDifficulty == GameDifficulty.Easy)
                    {
                        Controls.Add(EasyTablePanel);
                        Controls.Remove(MediumTablePanel);
                        Controls.Remove(HardTablePanel);
                        _buttonGrid = _buttonGridEasy;
                    }

                    if (_model.GameDifficulty == GameDifficulty.Medium)
                    {
                        Controls.Remove(EasyTablePanel);
                        Controls.Add(MediumTablePanel);
                        Controls.Remove(HardTablePanel);
                        _buttonGrid = _buttonGridMedium;
                    }

                    if (_model.GameDifficulty == GameDifficulty.Hard)
                    {
                        Controls.Remove(EasyTablePanel);
                        Controls.Remove(MediumTablePanel);
                        Controls.Add(HardTablePanel);
                        _buttonGrid = _buttonGridHard;
                    }

                    _pauseGameOption.Enabled = true;
                    _resumeGameOption.Enabled = false;

                    KeyDown -= new KeyEventHandler(StealthGameForm_KeyDown);
                    KeyDown += new KeyEventHandler(StealthGameForm_KeyDown);

                    SetupTable();
                    SetupMenus();

                    _timer.Start();
                }
                else
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Player moves event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StealthGameForm_KeyDown(object sender, KeyEventArgs e)
        {
            int p_row = _model.Table.GetPlayerCoords()[0];
            int p_col = _model.Table.GetPlayerCoords()[1];

            _exitReached = false;
            _detected = false;

            if (e.KeyCode == Keys.W && _model.Table.IsValidField(p_row - 1, p_col) && !_model.Table.IsWall(p_row - 1, p_col) && !_model.Table.IsGuard(p_row - 1, p_col))
            {
                _buttonGrid[p_row, p_col].BackgroundImage = null;
                _buttonGrid[p_row - 1, p_col].BackgroundImage = Image.FromFile(@"../../../Icons/player.png");
                _buttonGrid[p_row - 1, p_col].BackgroundImageLayout = ImageLayout.Center;

                _exitReached = _model.Table.IsExit(p_row - 1, p_col);
                _model.ExitReached = _exitReached;

                _detected = _model.VisionConeArea[p_row - 1, p_col] == 1;
                _model.Detected = _detected;

                _model.MovePlayer(p_row - 1, p_col);
            }
            else if (e.KeyCode == Keys.D && _model.Table.IsValidField(p_row, p_col + 1) && !_model.Table.IsWall(p_row, p_col + 1) && !_model.Table.IsGuard(p_row, p_col + 1))
            {
                _buttonGrid[p_row, p_col].BackgroundImage = null;
                _buttonGrid[p_row, p_col + 1].BackgroundImage = Image.FromFile(@"../../../Icons/player.png");
                _buttonGrid[p_row, p_col + 1].BackgroundImageLayout = ImageLayout.Center;

                _exitReached = _model.Table.IsExit(p_row, p_col + 1);
                _model.ExitReached = _exitReached;

                _detected = _model.VisionConeArea[p_row, p_col + 1] == 1;
                _model.Detected = _detected;

                _model.MovePlayer(p_row, p_col + 1);
            }
            else if (e.KeyCode == Keys.S && _model.Table.IsValidField(p_row + 1, p_col) && !_model.Table.IsWall(p_row + 1, p_col) && !_model.Table.IsGuard(p_row + 1, p_col))
            {
                _buttonGrid[p_row, p_col].BackgroundImage = null;
                _buttonGrid[p_row + 1, p_col].BackgroundImage = Image.FromFile(@"../../../Icons/player.png");
                _buttonGrid[p_row + 1, p_col].BackgroundImageLayout = ImageLayout.Center;

                _exitReached = _model.Table.IsExit(p_row + 1, p_col);
                _model.ExitReached = _exitReached;

                _detected = _model.VisionConeArea[p_row + 1, p_col] == 1;
                _model.Detected = _detected;

                _model.MovePlayer(p_row + 1, p_col);
            }
            else if (e.KeyCode == Keys.A && _model.Table.IsValidField(p_row, p_col - 1) && !_model.Table.IsWall(p_row, p_col - 1) && !_model.Table.IsGuard(p_row, p_col - 1))
            {
                _buttonGrid[p_row, p_col].BackgroundImage = null;
                _buttonGrid[p_row, p_col - 1].BackgroundImage = Image.FromFile(@"../../../Icons/player.png");
                _buttonGrid[p_row, p_col - 1].BackgroundImageLayout = ImageLayout.Center;

                _exitReached = _model.Table.IsExit(p_row, p_col - 1);
                _model.ExitReached = _exitReached;

                _detected = _model.VisionConeArea[p_row, p_col - 1] == 1;
                _model.Detected = _detected;

                _model.MovePlayer(p_row, p_col - 1);
            }
            else
            {
                return;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Generates the easy difficulty table.
        /// </summary>
        private void GenerateEasyTable()
        {
            _buttonGridEasy = new Button[_model.Table.EasySize, _model.Table.EasySize];

            for (int i = 0; i < _model.Table.EasySize; ++i)
                for (int j = 0; j < _model.Table.EasySize; ++j)
                {
                    _buttonGridEasy[i, j] = new Button
                    {
                        Size = new Size(30, 30),
                        Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                        TabIndex = 100 + i * _model.Table.EasySize + j,
                        FlatStyle = FlatStyle.Flat,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        Enabled = false
                    };

                    // Add to controls
                    EasyTablePanel.Controls.Add(_buttonGridEasy[i, j]);
                }
        }

        /// <summary>
        /// Generates the medium difficulty table.
        /// </summary>
        private void GenerateMediumTable()
        {
            _buttonGridMedium = new Button[_model.Table.MediumSize, _model.Table.MediumSize];

            for (int i = 0; i < _model.Table.MediumSize; ++i)
                for (int j = 0; j < _model.Table.MediumSize; ++j)
                {
                    _buttonGridMedium[i, j] = new Button
                    {
                        Size = new Size(30, 30),
                        Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                        TabIndex = 100 + i * _model.Table.MediumSize + j,
                        FlatStyle = FlatStyle.Flat,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        Enabled = false
                    };

                    // Add to controls
                    MediumTablePanel.Controls.Add(_buttonGridMedium[i, j]);
                }
        }

        /// <summary>
        /// Generates the hard difficulty table.
        /// </summary>
        private void GenerateHardTable()
        {
            _buttonGridHard = new Button[_model.Table.HardSize, _model.Table.HardSize];

            for (int i = 0; i < _model.Table.HardSize; ++i)
                for (int j = 0; j < _model.Table.HardSize; ++j)
                {
                    _buttonGridHard[i, j] = new Button
                    {
                        Size = new Size(30, 30),
                        Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                        TabIndex = 100 + i * _model.Table.HardSize + j,
                        FlatStyle = FlatStyle.Flat,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        Enabled = false
                    };

                    // Add to controls
                    HardTablePanel.Controls.Add(_buttonGridHard[i, j]);
                }
        }

        /// <summary>
        /// Generates each difficulty tables.
        /// </summary>
        private void GenerateTables()
        {
            GenerateEasyTable();
            GenerateMediumTable();
            GenerateHardTable();
        }

        /// <summary>
        /// Sets table fields based on values.
        /// </summary>
        private void SetupTable()
        {
            // Remove leftover player and guard positions
            for (int i = 0; i < _buttonGrid.GetLength(0); ++i)
                for (int j = 0; j < _buttonGrid.GetLength(1); ++j)
                {
                    _buttonGrid[i, j].BackgroundImage = null;
                }

            for (int i = 0; i < _buttonGrid.GetLength(0); ++i)
                for (int j = 0; j < _buttonGrid.GetLength(1); ++j)
                {
                    // Set player
                    if (_model.Table.IsPlayer(i, j))
                    {
                        _buttonGrid[i, j].BackgroundImage = Image.FromFile(@"../../../Icons/player.png");
                        _buttonGrid[i, j].BackgroundImageLayout = ImageLayout.Center;
                    }

                    if (_model.Table.IsExit(i, j))
                        _buttonGrid[i, j].BackColor = Color.Green;
                    if (_model.Table.IsWall(i, j))
                        _buttonGrid[i, j].BackColor = Color.Black;
                    
                    // Set guard coordinates and guard vision cones
                    if (_model.Table.IsGuard(i, j))
                    {
                        _buttonGrid[i, j].BackgroundImage = Image.FromFile(@"../../../Icons/guard.png");
                        _buttonGrid[i, j].BackgroundImageLayout = ImageLayout.Center;
                        _buttonGrid[i, j].BackColor = Color.LightBlue;
                        _model.SetVisionCone(i, j, _model.Table);
                    }
                }

            // Color vision cone and floor areas
            for (int i = 0; i < _buttonGrid.GetLength(0); ++i)
                for (int j = 0; j < _buttonGrid.GetLength(1); ++j)
                {
                    if (_model.Table.IsVision(i, j))
                        _buttonGrid[i, j].BackColor = Color.LightBlue;
                    if (_model.Table.IsFloor(i, j))
                        _buttonGrid[i, j].BackColor = Color.White;
                }
        }

        /// <summary>
        /// Refreshes vision cone areas.
        /// </summary>
        private void RefreshVisionCone()
        {
            for (int i = 0; i < _model.Table.TableSize; ++i)
                for (int j = 0; j < _model.Table.TableSize; ++j)
                {
                    if (_model.Table.IsGuard(i, j))
                        _model.SetVisionCone(i, j, _model.Table);

                }
        }

        /// <summary>
        /// Refreshes guard positions.
        /// </summary>
        private void RefreshGuards()
        {
            for (int i = 0; i < _model.Table.TableSize; ++i)
                for (int j = 0; j < _model.Table.TableSize; ++j)
                {
                    if (_model.Table.IsGuard(i, j))
                    {
                        _buttonGrid[i, j].BackgroundImage = Image.FromFile(@"../../../Icons/guard.png");
                        _buttonGrid[i, j].BackgroundImageLayout = ImageLayout.Center;
                        _buttonGrid[i, j].BackColor = Color.LightBlue;
                    }
                    else if (!_model.Table.IsPlayer(i, j))
                    {
                        _buttonGrid[i, j].BackgroundImage = null;
                    }

                    if (_model.VisionConeArea[i, j] == 1)
                    {
                        _buttonGrid[i, j].BackColor = Color.LightBlue;
                    }
                    else if (_model.VisionConeArea[i, j] == 2)
                    {
                        _buttonGrid[i, j].BackgroundImage = Image.FromFile(@"../../../Icons/player.png");
                        _buttonGrid[i, j].BackColor = Color.LightBlue;
                    }
                    else if (_model.Table.IsExit(i, j))
                    {
                        _buttonGrid[i, j].BackColor = Color.Green;
                    }
                    else if (_model.Table.IsFloor(i, j))
                    {
                        _buttonGrid[i, j].BackColor = Color.White;
                    }
                }
        }

        /// <summary>
        /// Set up the difficulty options.
        /// </summary>
        private void SetupMenus()
        {
            _easyOption.Checked = (_model.GameDifficulty == GameDifficulty.Easy);
            _mediumOption.Checked = (_model.GameDifficulty == GameDifficulty.Medium);
            _hardOption.Checked = (_model.GameDifficulty == GameDifficulty.Hard);
        }

        #endregion
    }
}
