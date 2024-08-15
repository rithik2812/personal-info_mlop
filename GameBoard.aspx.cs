using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Services;

namespace chesschess
{
    public partial class GameBoard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                InitializeGame();
            }
        }

        protected void NewGameButton_Click(object sender, EventArgs e)
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            ChessGame game = new ChessGame();
            Session["GameState"] = game;
        }

        [WebMethod]
        public static object GetBoardState()
        {
            ChessGame game = (ChessGame)HttpContext.Current.Session["GameState"];
            return new
            {
                pieces = game.Pieces
            };
        }

        [WebMethod]
        public static object MovePiece(int pieceId, int targetSquare)
        {
            ChessGame game = (ChessGame)HttpContext.Current.Session["GameState"];
            ChessPiece piece = game.Pieces.Find(p => p.Position == pieceId);

            if (piece != null)
            {
                bool isWhitePiece = piece.Symbol.StartsWith("♙") || piece.Symbol.StartsWith("♖") || piece.Symbol.StartsWith("♘") || piece.Symbol.StartsWith("♗") || piece.Symbol.StartsWith("♕") || piece.Symbol.StartsWith("♔");
                bool isBlackPiece = !isWhitePiece;

                if ((game.IsWhiteTurn && isWhitePiece) || (!game.IsWhiteTurn && isBlackPiece))
                {
                    if (IsMoveValid(piece, targetSquare, game))
                    {
                        ChessPiece targetPiece = game.Pieces.Find(p => p.Position == targetSquare);
                        if (targetPiece != null)
                        {
                            game.CapturedPieces.Add(targetPiece);
                            game.Pieces.Remove(targetPiece);
                        }

                        piece.Position = targetSquare;
                        game.IsWhiteTurn = !game.IsWhiteTurn;
                        HttpContext.Current.Session["GameState"] = game;
                        return new { success = true };
                    }
                }
            }

            return new { success = false };
        }

        [WebMethod]
        public static object GetCurrentTurn()
        {
            ChessGame game = (ChessGame)HttpContext.Current.Session["GameState"];
            return new { isWhiteTurn = game.IsWhiteTurn };
        }

        private static bool IsMoveValid(ChessPiece piece, int targetSquare, ChessGame game)
        {
            int currentRow = piece.Position / 8;
            int currentCol = piece.Position % 8;
            int targetRow = targetSquare / 8;
            int targetCol = targetSquare % 8;

            int rowDifference = Math.Abs(targetRow - currentRow);
            int colDifference = Math.Abs(targetCol - currentCol);

            ChessPiece targetPiece = game.Pieces.Find(p => p.Position == targetSquare);

            switch (piece.Name)
            {
                case "Pawn":
                    if (piece.Symbol == "♙")
                    {
                        if (currentRow == 1 && targetRow == 3 && colDifference == 0 && targetPiece == null && IsEmptySquare(targetRow, targetCol, game))
                            return true;
                        if (targetRow == currentRow + 1 && colDifference == 0 && targetPiece == null)
                            return true;
                        if (targetRow == currentRow + 1 && colDifference == 1 && targetPiece != null && targetPiece.Symbol.StartsWith("♟"))
                            return true; 
                        if (targetRow == currentRow + 1 && colDifference == 1 && targetPiece == null && IsEnPassant(piece.Position, targetSquare, game))
                            return true;
                    }
                    else if (piece.Symbol == "♟") 
                    {
                        if (currentRow == 6 && targetRow == 4 && colDifference == 0 && targetPiece == null && IsEmptySquare(targetRow, targetCol, game))
                            return true;
                        if (targetRow == currentRow - 1 && colDifference == 0 && targetPiece == null)
                            return true;
                        if (targetRow == currentRow - 1 && colDifference == 1 && targetPiece != null && targetPiece.Symbol.StartsWith("♙"))
                            return true;
                        if (targetRow == currentRow - 1 && colDifference == 1 && targetPiece == null && IsEnPassant(piece.Position, targetSquare, game))
                            return true;
                    }
                    break;

                case "Rook":
                    if ((rowDifference == 0 || colDifference == 0) && IsPathClear(piece.Position, targetSquare, game))
                        return true;
                    break;

                case "Knight":
                    if ((rowDifference == 2 && colDifference == 1) || (rowDifference == 1 && colDifference == 2))
                        return true;
                    break;

                case "Bishop":
                    if (rowDifference == colDifference && IsPathClear(piece.Position, targetSquare, game))
                        return true;
                    break;

                case "Queen":
                    if ((rowDifference == colDifference || rowDifference == 0 || colDifference == 0) && IsPathClear(piece.Position, targetSquare, game))
                        return true;
                    break;

                case "King":
                    if (rowDifference <= 1 && colDifference <= 1)
                        return true;
                    if (IsCastling(piece, targetSquare, game))
                        return true;
                    break;
            }

            return false;
        }

        private static bool IsPathClear(int startSquare, int endSquare, ChessGame game)
        {
            int startRow = startSquare / 8;
            int startCol = startSquare % 8;
            int endRow = endSquare / 8;
            int endCol = endSquare % 8;

            int rowDirection = Math.Sign(endRow - startRow);
            int colDirection = Math.Sign(endCol - startCol);

            int currentRow = startRow + rowDirection;
            int currentCol = startCol + colDirection;

            while (currentRow != endRow || currentCol != endCol)
            {
                int squareIndex = currentRow * 8 + currentCol;
                if (game.Pieces.Exists(p => p.Position == squareIndex))
                {
                    return false; // Path is not clear
                }

                currentRow += rowDirection;
                currentCol += colDirection;
            }

            return true;
        }

        private static bool IsEnPassant(int pawnPosition, int targetSquare, ChessGame game)
        {
            int targetRow = targetSquare / 8;
            int targetCol = targetSquare % 8;
            int pawnRow = pawnPosition / 8;
            int pawnCol = pawnPosition % 8;

            if (targetRow == pawnRow + 1 || targetRow == pawnRow - 1)
            {
                if (targetCol == pawnCol + 1 || targetCol == pawnCol - 1)
                {
                    ChessPiece adjacentPawn = game.Pieces.Find(p => p.Position == (pawnRow * 8) + targetCol);
                    return adjacentPawn != null && adjacentPawn.Name == "Pawn" && adjacentPawn.Symbol != game.Pieces.Find(p => p.Position == pawnPosition).Symbol;
                }
            }

            return false;
        }

        private static bool IsCastling(ChessPiece king, int targetSquare, ChessGame game)
        {
            if (king.Name != "King")
                return false;

            int currentRow = king.Position / 8;
            int targetRow = targetSquare / 8;

            if (currentRow != targetRow)
                return false;

            int currentCol = king.Position % 8;
            int targetCol = targetSquare % 8;

            if (Math.Abs(targetCol - currentCol) != 2)
                return false;

            int rookCol = targetCol > currentCol ? 7 : 0;
            ChessPiece rook = game.Pieces.Find(p => p.Name == "Rook" && p.Position == (currentRow * 8) + rookCol);

            if (rook == null || rook.Symbol.StartsWith("♟") == king.Symbol.StartsWith("♟"))
                return false;

            int colDirection = targetCol > currentCol ? 1 : -1;
            for (int col = currentCol + colDirection; col != targetCol; col += colDirection)
            {
                if (game.Pieces.Exists(p => p.Position == (currentRow * 8) + col))
                {
                    return false; 
                }
            }

            bool kingMoved = game.Pieces.Exists(p => p.Name == "King" && p.Position != king.Position);
            bool rookMoved = game.Pieces.Exists(p => p.Name == "Rook" && p.Position != rook.Position);

            if (kingMoved || rookMoved)
                return false;

            return true;
        }

        private static bool IsEmptySquare(int row, int col, ChessGame game)
        {
            return !game.Pieces.Exists(p => p.Position == (row * 8) + col);
        }
    }

    public class ChessGame
    {
        public List<ChessPiece> Pieces { get; set; }
        public List<ChessPiece> CapturedPieces { get; set; }
        public bool IsWhiteTurn { get; set; }

        public ChessGame()
        {
            Pieces = InitializeBoard();
            CapturedPieces = new List<ChessPiece>(); 
            IsWhiteTurn = true; 
        }

        private List<ChessPiece> InitializeBoard()
        {
            var pieces = new List<ChessPiece>();

            pieces.Add(new ChessPiece("Rook", "♖", 0));   
            pieces.Add(new ChessPiece("Knight", "♘", 1)); 
            pieces.Add(new ChessPiece("Bishop", "♗", 2)); 
            pieces.Add(new ChessPiece("Queen", "♕", 3)); 
            pieces.Add(new ChessPiece("King", "♔", 4));   
            pieces.Add(new ChessPiece("Bishop", "♗", 5)); 
            pieces.Add(new ChessPiece("Knight", "♘", 6)); 
            pieces.Add(new ChessPiece("Rook", "♖", 7));   
            for (int i = 8; i < 16; i++) pieces.Add(new ChessPiece("Pawn", "♙", i));

            pieces.Add(new ChessPiece("Rook", "♜", 56));  
            pieces.Add(new ChessPiece("Knight", "♞", 57));
            pieces.Add(new ChessPiece("Bishop", "♝", 58));
            pieces.Add(new ChessPiece("Queen", "♛", 59)); 
            pieces.Add(new ChessPiece("King", "♚", 60));  
            pieces.Add(new ChessPiece("Bishop", "♝", 61));
            pieces.Add(new ChessPiece("Knight", "♞", 62));
            pieces.Add(new ChessPiece("Rook", "♜", 63)); 
            for (int i = 48; i < 56; i++) pieces.Add(new ChessPiece("Pawn", "♟", i)); 
            return pieces;
        }
    }

    public class ChessPiece
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Position { get; set; }

        public ChessPiece(string name, string symbol, int position)
        {
            Name = name;
            Symbol = symbol;
            Position = position;
        }
    }
}
