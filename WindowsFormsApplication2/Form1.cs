using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        private static ChessBoardSquare[,] squares = new ChessBoardSquare[8, 8];
        private static Position currentHighlight;
        private static List<Position> possiblePositions;
        private static Dictionary<ChessPiece, Icon> resourceMap;
        private static Dictionary<Position, ChessPiece> pieceMap; // to determine which piece is where.
        private static bool isWhitesTurn;                         // flag to determine whose turn it is currently.
        private static bool isKingInCheck;                          // flag to determine if any of the 2 kings is threatened.
        private static ToolStripLabel statusLabel;
        private static CheckInfo info;

        public Form1()
        {
            InitializeComponent();
            isWhitesTurn = true;
            isKingInCheck = false;
            currentHighlight = null;
            possiblePositions = new List<Position>();
            pieceMap = new Dictionary<Position, ChessPiece>();
            statusLabel = new ToolStripLabel();
            info = null;

            //adding status label to the status strip, cant be added via the designer because this is a static object.
            statusStrip1.Items.Add(statusLabel);

            tableLayoutPanel1.RowCount = 8;
            tableLayoutPanel1.ColumnCount = 8;
            for (int i = 0; i < tableLayoutPanel1.RowCount; i++)
            {
                for (int j = 0; j < tableLayoutPanel1.ColumnCount; j++)
                {
                    squares[i, j] = new ChessBoardSquare(i, j);
                    tableLayoutPanel1.Controls.Add(squares[i,j],j,i);
                }
            }

            // defining the resource dictionary
            resourceMap = new Dictionary<ChessPiece, Icon>();
            resourceMap.Add(ChessPiece.WhiteRook, WindowsFormsApplication2.Properties.Resources.White_Rook);
            resourceMap.Add(ChessPiece.WhiteHorse, WindowsFormsApplication2.Properties.Resources.White_Horse);
            resourceMap.Add(ChessPiece.WhiteBishop, WindowsFormsApplication2.Properties.Resources.White_Bishop);
            resourceMap.Add(ChessPiece.WhiteKing, WindowsFormsApplication2.Properties.Resources.White_King);
            resourceMap.Add(ChessPiece.WhiteQueen, WindowsFormsApplication2.Properties.Resources.White_Queen);
            resourceMap.Add(ChessPiece.WhitePawn, WindowsFormsApplication2.Properties.Resources.White_Pawn);
            resourceMap.Add(ChessPiece.BlackRook, WindowsFormsApplication2.Properties.Resources.Black_Rook);
            resourceMap.Add(ChessPiece.BlackHorse, WindowsFormsApplication2.Properties.Resources.Black_Horse);
            resourceMap.Add(ChessPiece.BlackBishop, WindowsFormsApplication2.Properties.Resources.Black_Bishop);
            resourceMap.Add(ChessPiece.BlackKing, WindowsFormsApplication2.Properties.Resources.Black_King);
            resourceMap.Add(ChessPiece.BlackQueen, WindowsFormsApplication2.Properties.Resources.Black_Queen);
            resourceMap.Add(ChessPiece.BlackPawn, WindowsFormsApplication2.Properties.Resources.Black_Pawn);

            // setting initial positions for pieces
            squares[0, 0].setPiece(ChessPiece.WhiteRook); 
            squares[0, 1].setPiece(ChessPiece.WhiteHorse); 
            squares[0, 2].setPiece(ChessPiece.WhiteBishop);
            squares[0, 3].setPiece(ChessPiece.WhiteKing);
            squares[0, 4].setPiece(ChessPiece.WhiteQueen);
            squares[0, 5].setPiece(ChessPiece.WhiteBishop);
            squares[0, 6].setPiece(ChessPiece.WhiteHorse);
            squares[0, 7].setPiece(ChessPiece.WhiteRook);

            squares[7, 0].setPiece(ChessPiece.BlackRook);
            squares[7, 1].setPiece(ChessPiece.BlackHorse);
            squares[7, 2].setPiece(ChessPiece.BlackBishop);
            squares[7, 3].setPiece(ChessPiece.BlackKing);
            squares[7, 4].setPiece(ChessPiece.BlackQueen);
            squares[7, 5].setPiece(ChessPiece.BlackBishop);
            squares[7, 6].setPiece(ChessPiece.BlackHorse);
            squares[7, 7].setPiece(ChessPiece.BlackRook);

            for (int i = 0; i < tableLayoutPanel1.ColumnCount; i++)
            {
                    squares[1, i].setPiece(ChessPiece.WhitePawn);
                    squares[6, i].setPiece(ChessPiece.BlackPawn);
            }

            setStatus("White's Move now");
        }


        public static void setStatus(string status)
        {
            clearErrorHighlight();      // clear error highlights if any
            statusLabel.Text = status;
        }

        public static Player getPlayerSide(ChessPiece piece)
        {
            switch(piece)
            {
                case ChessPiece.WhiteHorse:
                case ChessPiece.WhiteKing:
                case ChessPiece.WhitePawn:
                case ChessPiece.WhiteQueen:
                case ChessPiece.WhiteRook:
                case ChessPiece.WhiteBishop:
                    return Player.White;
                case ChessPiece.None:
                    Console.WriteLine("[!]FATAL ERROR: Form1.getPlayerSide() has returned ERR");
                    return Player.ERR;      // if this happens, something is seriously wrong :(
                default:
                    return Player.Black;
            }
        }

        public static bool isCapturePosition(Position pos) // determines if the specificied position pos is a capture position relative to currentHighlightPosition
        {
            if (squares[pos.Row,pos.Col].isEmpty())
                return false;
            else
            {
                // checking whether both pieces belong to the same side.....
                if (getPlayerSide(squares[currentHighlight.Row, currentHighlight.Col].getPiece()) == getPlayerSide(squares[pos.Row,pos.Col].getPiece()))
                    return false;
                else
                    return true;
            }
        }

        public static bool isCapturePosition(Position target,Position reference) // checks if target position is a capture position relative to provided reference position
        {
            if (squares[target.Row, target.Col].isEmpty())
                return false;
            else
            {
                if (getPlayerSide(squares[reference.Row, reference.Col].getPiece()) == getPlayerSide(squares[target.Row, target.Col].getPiece()))
                    return false;
                else
                    return true;
            }
        }

        public static void clearHighlights()
        {
            if(!Object.ReferenceEquals(currentHighlight,null) || currentHighlight != null )
            {
                // removing highlights
                squares[currentHighlight.Row, currentHighlight.Col].toggleHighlight();
                foreach (Position pos in possiblePositions)
                {
                    squares[pos.Row, pos.Col].toggleHighlight();
                }

                // disposing highlight objects
                currentHighlight = null;
                possiblePositions.Clear();
            }
        }

        public static bool isEmptyPosition(Position pos)  // determines whether the Position pos is empty(no pieces) on the board
        {
            return squares[pos.Row, pos.Col].isEmpty();
        }

        public static void highlightPossiblePositions()
        {
            ChessPiece piece =  squares[currentHighlight.Row, currentHighlight.Col].getPiece();
            switch(piece)
            {
                case ChessPiece.None:
                    break;
                case ChessPiece.WhitePawn:
                    Position possiblePos;
                    if (currentHighlight.Row == 1) // check if pawn is in initial position
                    {
                        possiblePos =new Position(currentHighlight.Row + 2, currentHighlight.Col);
                        if (isEmptyPosition(possiblePos) && isEmptyPosition(possiblePos+new Position(-1,0)))
                        {
                            possiblePositions.Add(possiblePos);
                        }
                    }
                    possiblePos = new Position(currentHighlight.Row + 1, currentHighlight.Col);
                    if (isEmptyPosition(possiblePos))
                    {
                        possiblePositions.Add(possiblePos);
                    }
                    // TODO:Add pawn cross capture to possible moves here...
                    possiblePos = new Position(currentHighlight.Row + 1, currentHighlight.Col-1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos))
                    {
                        possiblePositions.Add(possiblePos);
                    }
                    possiblePos = new Position(currentHighlight.Row + 1, currentHighlight.Col+1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos))
                    {
                        possiblePositions.Add(possiblePos);
                    }
                    break;
                case ChessPiece.BlackPawn:
                    //Position possiblePos;
                    if (currentHighlight.Row == 6) // check if pawn is in initial position
                    {
                        possiblePos =new Position(currentHighlight.Row - 2, currentHighlight.Col);
                        if (isEmptyPosition(possiblePos) && isEmptyPosition(possiblePos+new Position(1,0))) // path should be clear when pawn is going to move 2 steps
                        {
                            possiblePositions.Add(possiblePos);
                        }
                    }
                    possiblePos = new Position(currentHighlight.Row - 1, currentHighlight.Col);
                    if (isEmptyPosition(possiblePos))
                    {
                        possiblePositions.Add(possiblePos);
                    }possiblePos = new Position(currentHighlight.Row - 1, currentHighlight.Col-1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos))
                    {
                        possiblePositions.Add(possiblePos);
                    }
                    possiblePos = new Position(currentHighlight.Row - 1, currentHighlight.Col+1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos))
                    {
                        possiblePositions.Add(possiblePos);
                    }
                    break;
                case ChessPiece.WhiteHorse:
                case ChessPiece.BlackHorse:
                    possiblePositions.AddRange(horseTransform());
                    break;
                case ChessPiece.BlackRook:
                case ChessPiece.WhiteRook:
                    possiblePositions.AddRange(rookTransform());
                    break;
                case ChessPiece.WhiteBishop:
                case ChessPiece.BlackBishop:
                    possiblePositions.AddRange(bishopTransform());
                    break;
                case ChessPiece.BlackQueen:
                case ChessPiece.WhiteQueen:
                    possiblePositions.AddRange(bishopTransform());
                    possiblePositions.AddRange(rookTransform());
                    break;
                case ChessPiece.BlackKing:
                case ChessPiece.WhiteKing:
                    possiblePositions.AddRange(kingTransform());
                    break;
            }
            foreach (Position pos in possiblePositions)
            {
                squares[pos.Row, pos.Col].toggleHighlight();
                if (isCapturePosition(pos))
                {
                    squares[pos.Row, pos.Col].setCaptureHighlight();
                }
            }
        }

        public static List<Position> getPossiblePositions(Position pos)
        {
            ChessPiece piece = squares[pos.Row, pos.Col].getPiece();
            List<Position> nPossiblePositions = new List<Position>();
            switch (piece)
            {
                case ChessPiece.None:
                    break;
                case ChessPiece.WhitePawn:
                    Position possiblePos;
                    if (pos.Row == 1) // check if pawn is in initial position
                    {
                        possiblePos = new Position(pos.Row + 2, pos.Col);
                        if (isEmptyPosition(possiblePos) && isEmptyPosition(possiblePos + new Position(-1, 0)))
                        {
                            nPossiblePositions.Add(possiblePos);
                        }
                    }
                    possiblePos = new Position(pos.Row + 1, pos.Col);
                    if (isEmptyPosition(possiblePos))
                    {
                        nPossiblePositions.Add(possiblePos);
                    }
                    // TODO:Add pawn cross capture to possible moves here...
                    possiblePos = new Position(pos.Row + 1, pos.Col - 1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos,pos))
                    {
                        nPossiblePositions.Add(possiblePos);
                    }
                    possiblePos = new Position(pos.Row + 1, pos.Col + 1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos,pos))
                    {
                        nPossiblePositions.Add(possiblePos);
                    }
                    break;
                case ChessPiece.BlackPawn:
                    //Position possiblePos;
                    if (pos.Row == 6) // check if pawn is in initial position
                    {
                        possiblePos = new Position(pos.Row - 2, pos.Col);
                        if (isEmptyPosition(possiblePos) && isEmptyPosition(possiblePos + new Position(1, 0))) // path should be clear when pawn is going to move 2 steps
                        {
                            nPossiblePositions.Add(possiblePos);
                        }
                    }
                    possiblePos = new Position(pos.Row - 1, pos.Col);
                    if (isEmptyPosition(possiblePos))
                    {
                        nPossiblePositions.Add(possiblePos);
                    } possiblePos = new Position(pos.Row - 1, pos.Col - 1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos,pos))
                    {
                        nPossiblePositions.Add(possiblePos);
                    }
                    possiblePos = new Position(pos.Row - 1, pos.Col + 1);
                    if (isValidPosition(possiblePos) && isCapturePosition(possiblePos,pos))
                    {
                        nPossiblePositions.Add(possiblePos);
                    }
                    break;
                case ChessPiece.WhiteHorse:
                case ChessPiece.BlackHorse:
                    nPossiblePositions.AddRange(horseTransform(pos));
                    break;
                case ChessPiece.BlackRook:
                case ChessPiece.WhiteRook:
                    nPossiblePositions.AddRange(rookTransform(pos));
                    break;
                case ChessPiece.WhiteBishop:
                case ChessPiece.BlackBishop:
                    nPossiblePositions.AddRange(bishopTransform(pos));
                    break;
                case ChessPiece.BlackQueen:
                case ChessPiece.WhiteQueen:
                    nPossiblePositions.AddRange(bishopTransform(pos));
                    nPossiblePositions.AddRange(rookTransform(pos));
                    break;
                case ChessPiece.BlackKing:
                case ChessPiece.WhiteKing:
                    nPossiblePositions.AddRange(kingTransform(pos));
                    break;
            }
            return nPossiblePositions;
        }

        private static List<Position> kingTransform()
        {
            return kingTransform(currentHighlight);
        }

        private static List<Position> kingTransform(Position pos)
        {
            List<Position> transform = new List<Position>();
            List<Position> temp = new List<Position>();
            temp.Add(pos + new Position(1, 1));
            temp.Add(pos + new Position(1, -1));
            temp.Add(pos + new Position(-1, 1));
            temp.Add(pos + new Position(-1, -1));
            temp.Add(pos + new Position(0, 1));
            temp.Add(pos + new Position(1, 0));
            temp.Add(pos + new Position(-1, 0));
            temp.Add(pos + new Position(0, -1));

            foreach(Position t in temp)
            {
                if(isValidPosition(t))
                {
                    if (isEmptyPosition(t) || isCapturePosition(t,pos))
                        transform.Add(t);
                }
            }
            temp.Clear();
            return transform;
        }

        private static List<Position> bishopTransform() // compatiblity for previous code
        {
            return bishopTransform(currentHighlight);
        }

        private static List<Position> bishopTransform(Position pos)
        {
            Position[] temp = new Position[4];
            List<Position> transform = new List<Position>();
            bool flag0 = false, flag1 = false, flag2 = false, flag3 = false;

            for (int i = 1; i < 8; i++)
            {
                if (!flag0)
                    temp[0] = pos + new Position(i, i);
                else
                    temp[0] = new Position(-1,-1);
                if(!flag1)
                    temp[1] = pos + new Position(i, -1 * i);
                else
                    temp[1] = new Position(-1, -1);
                if(!flag2)
                    temp[2] = pos + new Position(i * -1, i);
                else
                    temp[2] = new Position(-1, -1);
                if(!flag3)
                    temp[3] = pos + new Position(i * -1, i * -1);
                else
                    temp[3] = new Position(-1, -1);
                for (int j = 0; j < temp.Length; j++)
                {
                    if (isValidPosition(temp[j]))
                    {
                        if (isEmptyPosition(temp[j]))
                            transform.Add(temp[j]);
                        else if (isCapturePosition(temp[j],pos))
                        {
                            transform.Add(temp[j]);
                            switch(j)
                            {
                                case 0:
                                    flag0 = true;
                                    break;
                                case 1:
                                    flag1 = true;
                                    break;
                                case 2:
                                    flag2 = true;
                                    break;
                                case 3:
                                    flag3 = true;
                                    break;
                            }
                        }
                        else
                        {
                            switch (j)
                            {
                                case 0:
                                    flag0 = true;
                                    break;
                                case 1:
                                    flag1 = true;
                                    break;
                                case 2:
                                    flag2 = true;
                                    break;
                                case 3:
                                    flag3 = true;
                                    break;
                            }
                        }
                    }
                }
            }
            return transform;
        }

        public static void makeMove(Position oldPosition, Position newPosition)
        {
            ChessPiece piece = squares[oldPosition.Row, oldPosition.Col].getPiece(); // obtained a reference to the checkpiece that's selected.
            if (oldPosition == newPosition) // stupid or accidental maybe ?
                return;
            if ((isWhitesTurn && getPlayerSide(piece) == Player.White) || (!isWhitesTurn && getPlayerSide(piece) == Player.Black))
            {
                ChessPiece capturedPiece = squares[newPosition.Row,newPosition.Col].getPiece();
                if(capturedPiece!=ChessPiece.None)
                {
                    // enemy piece captured.
                    // TODO: Add the captured pieces to the opponent's capture List.
                }
                clearHighlights();
                squares[oldPosition.Row, oldPosition.Col].removeImage();
                squares[newPosition.Row, newPosition.Col].setPiece(piece);
                // TODO: king in check ? notify by highlighting in red.
                kingInCheck(newPosition);
                isWhitesTurn = !isWhitesTurn;
                updateStatus();
            }
            else
            {
                setErrorHighlight();
                highlightStatus();
            }
        }

        public static void  kingInCheck(Position position)  // checks if the piece in the provided position poses a check to the opposite king.
        {
            ChessPiece piece = squares[position.Row, position.Col].getPiece(); // the piece which had been moved now.
            ChessPiece enemyKing;
            Position enemyKingPosition = null ;
            if (getPlayerSide(piece) == Player.White)
                enemyKing = ChessPiece.BlackKing;
            else
                enemyKing = ChessPiece.WhiteKing;

            // getting position of enemy king.
            foreach(Position p in pieceMap.Keys)
            {
                if(pieceMap[p] == enemyKing)
                {
                    Debug.Print("Position : "+p);
                    enemyKingPosition = p;
                    break;
                }
            }

            if(enemyKingPosition == null)
            {
                setErrorHighlight();
                Debug.Print("cidjf");
                setStatus("[Debug] FATAL: Enemy King position cannot be determined");
                return;
            }

            foreach(Position possiblePosition in getPossiblePositions(position))
            {
                Debug.Print("Position : " + possiblePosition);
                if(possiblePosition == enemyKingPosition)
                {
                    setStatus("King in check!");
                    // setting check info - required later for checking. (and ofcourse, removing checkHighlight)
                    if (info == null)
                        info = new CheckInfo();
                    info.threatenedBy = position;
                    info.pieceInCheck = enemyKingPosition;
                    //TODO: while making move check both if the move leads to a check or if the king is in check, the move should clear the check.
                    // ---
                    isKingInCheck = true;
                    squares[enemyKingPosition.Row, enemyKingPosition.Col].setCheckHighlight();
                }
            }
        }

        public static async void setErrorHighlight()
        {
            statusLabel.BackColor = Color.Red;
            await Task.Delay(TimeSpan.FromSeconds(3));
            clearErrorHighlight();
        }

        public static void clearErrorHighlight()
        {
            statusLabel.BackColor = Color.Transparent;
        }

        public static void highlightStatus()
        {
            statusLabel.ForeColor = Color.Black;
            statusLabel.Font = new Font(SystemFonts.StatusFont, FontStyle.Bold);
        }

        public static void clearHighlightStatus()
        {
            statusLabel.ForeColor = Color.Black;
            statusLabel.Font = new Font(SystemFonts.StatusFont, FontStyle.Regular);
        }

        private static void updateStatus()
        {
            clearHighlightStatus();
            if (isWhitesTurn)
                setStatus("White's Turn now");
            else
                setStatus("Black's Turn now");
        }

        public static List<Position> rookTransform()
        {
            return rookTransform(currentHighlight);
        }

        public static List<Position> rookTransform(Position pos)
        {
            Position temp;
            List<Position> transform = new List<Position>();

            for(int i=pos.Row+1;i<8;i++)
            {
                temp = new Position(i,pos.Col);
                if (isEmptyPosition(temp))
                    transform.Add(temp);
                else
                {
                    if (isCapturePosition(temp,pos))
                        transform.Add(temp);
                    break;
                }
            }
            for (int i = pos.Row - 1; i >= 0; i--)
            {
                temp = new Position(i, pos.Col);
                if (isEmptyPosition(temp))
                    transform.Add(temp);
                else
                {
                    if (isCapturePosition(temp,pos))
                        transform.Add(temp);
                    break;
                }
            }
            for (int i = pos.Col + 1; i < 8; i++)
            {
                temp = new Position(pos.Row, i);
                if (isEmptyPosition(temp))
                    transform.Add(temp);
                else
                {
                    if (isCapturePosition(temp,pos))
                        transform.Add(temp);
                    break;
                }
            }
            for (int i = pos.Col - 1; i >= 0; i--)
            {
                temp = new Position(pos.Row, i);
                if (isEmptyPosition(temp))
                    transform.Add(temp);
                else
                {
                    if (isCapturePosition(temp,pos))
                        transform.Add(temp);
                    break;
                }
            }
            return transform;
        }


        public static List<Position> horseTransform()
        {
            return horseTransform(currentHighlight);
        }
        public static List<Position> horseTransform(Position pos)
        {
            List<Position> transform = new List<Position>();
            int x = 1, y = 2, count = 0;
            while (count < 2)
            {
                transform.Add(new Position(pos.Row + x, pos.Col + y));
                transform.Add(new Position(pos.Row - x, pos.Col + y));
                transform.Add(new Position(pos.Row + x, pos.Col - y));
                transform.Add(new Position(pos.Row - x, pos.Col - y));
                swap(ref x,ref y);
                count++;
            }

            List<Position> finalList = new List<Position>();
            //check if all the transforms are valid positions and empty
            foreach(Position t in transform)
            {
                if(isValidPosition(t) && (squares[t.Row,t.Col].isEmpty() || isCapturePosition(t,pos)))
                {
                    finalList.Add(t);
                }
            }
            transform.Clear();
            return finalList;
        }

        private static bool isValidPosition(Position pos)
        {
            return ((pos.Row >= 0 && pos.Row <= 7) && (pos.Col >= 0 && pos.Col <= 7));
        }

        public static void swap(ref int x,ref int y)
        {
            int temp;
            temp = x;
            x = y;
            y = temp;
        }
        class ChessBoardSquare : Panel
        {
            private Position position;
            private PictureBox pictureBox;
            private static Color highlightColor = Color.LightGreen;
            private static Color captureColor = Color.Tomato;
            private ChessPiece piece;

            public int Row
            {
                get { return this.position.Row; }
                set {
                    if(this.position == null)
                    {
                        this.position = new Position(0,0);
                    }
                    this.position.Row = value; }
            }
            public int Col
            {
                get { return this.position.Col; }
                set { this.position.Col = value; }
            }

            public Position Pos
            {
                get { return new Position(this.Row, this.Col); }
                set { this.position = new Position(value.Row, value.Col); }
            }
            private Color getInitialColor()
            {
                if ((this.Row + this.Col) % 2 == 0)
                    return Color.Black;
                else
                    return Color.Beige;
            }
            public ChessBoardSquare(int row, int col)
            {
                this.Row = row;
                this.Col = col;
                this.piece = ChessPiece.None;

                this.BackColor = this.getInitialColor();

                this.Margin = new Padding(0);
                this.pictureBox = new PictureBox();
                this.pictureBox.Width = this.Width;
                this.pictureBox.Height = this.Height;
                this.pictureBox.BackColor = this.BackColor;
                this.BorderStyle = BorderStyle.FixedSingle;

                // adding controls
                this.Controls.Add(pictureBox);

                // adding event handlers
                this.pictureBox.Click += new EventHandler(handleClick);
                this.BackColorChanged += new EventHandler(handleColorChange);
            }
            public void setPiece(ChessPiece piece)
            {
                pieceMap.Add(this.getPosition(),piece);
                this.piece = piece;
                this.setImage(resourceMap[piece]);
            }
            public void setImage(Icon icon)
            {

                Bitmap bitmap = new Bitmap(this.Width, this.Height);
                Graphics gx = Graphics.FromImage(bitmap);
                gx.DrawIcon(icon, this.Width / 4, this.Height / 4);
                gx.Dispose();
                this.pictureBox.Image = bitmap;
            }

            public void setImage(Image img)
            {
                this.pictureBox.Image = img;
            }
            public void handleClick(object sender, EventArgs args)
            {
                if (isHighlighted())
                {
                    Position old = currentHighlight;
                    makeMove(old, this.Pos);
                }
                else
                {
                    clearHighlights();
                    currentHighlight = new Position(this.Row, this.Col);
                    Console.WriteLine(currentHighlight);
                    toggleHighlight();
                    highlightPossiblePositions();
                }
            }
            public void handleColorChange(object sender, EventArgs args)
            {
                this.pictureBox.BackColor = this.BackColor;
            }
            public void toggleHighlight()
            {
                if (isHighlighted())
                {
                    this.BackColor = this.getInitialColor();
                }
                else
                {
                    this.BackColor = highlightColor;
                }
            }

            public void setCaptureHighlight()
            {
                this.BackColor = captureColor;
            }

            private bool isHighlighted()
            {
                return (this.BackColor.Equals(highlightColor) || this.BackColor.Equals(captureColor));
            }

            public bool isEmpty() // does not contain any chess pieces at this square
            {
                return (this.piece == ChessPiece.None);
            }

            public ChessPiece getPiece()
            {
                return this.piece;
            }

            public Position getPosition()
            {
                return new Position(this.Row, this.Col);
            }

            public void removeImage()
            {
                this.pictureBox.Image = null;
                pieceMap.Remove(this.Pos);
                this.piece = ChessPiece.None;
            }
            public void setCheckHighlight()
            {
                this.BackColor = Color.Red;
            }
        }
    }
    public class Position:Object
    {
        private int row, col;
        public int Row
        {
            get { return this.row; }
            set { this.row = value; }
        }

        public int Col
        {
            get { return this.col; }
            set { this.col = value; }
        }

        public Position(int x, int y)
        {
            this.Row = x;
            this.Col = y;
        }

        public static Position operator +(Position pos1,Position pos2)
        {
            return new Position(pos1.Row + pos2.Row, pos1.Col + pos2.Col);
        }

        public static Position operator -(Position pos1,Position pos2)
        {
            return new Position(pos1.Row - pos2.Row, pos1.Col - pos2.Col);
        }

        public static bool operator ==(Position pos1, Position pos2)
        {
            if (ReferenceEquals(pos1, null))
            {
                if (ReferenceEquals(pos2, null))
                {
                    return true;
                }
                return false;
            }
            else
            {
                return pos1.Equals(pos2);
            }
        }

        public static bool operator !=(Position pos1, Position pos2)
        {
            return !(pos1 == pos2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                Position pos = (Position)obj;
                return (this.Row == pos.Row && this.Col == pos.Col);
            }
        }

        public override string ToString()
        {
            return "[" + this.Row + "," + this.Col + "]";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public enum ChessPiece { WhiteRook  , WhiteHorse  , WhiteKing  , WhiteQueen  , WhiteBishop  , WhitePawn  , BlackRook  , BlackHorse  , BlackKing  , BlackQueen  , BlackBishop  , BlackPawn, None }
    public enum Player { White,Black,ERR }

    public class CheckInfo
    {
        public Position pieceInCheck, threatenedBy; //  chess piece at that position can be obtained, so no need for explicit chesspiece declaration here
    }
}
