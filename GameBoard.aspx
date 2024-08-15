<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GameBoard.aspx.cs" Inherits="chesschess.GameBoard" Async="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>CHESS GAME</title>
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://code.jquery.com/ui/1.12.1/jquery-ui.min.js"></script>
    <link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/base/jquery-ui.css">
    <style>
        #chessBoard {
            width: 400px;
            height: 400px;
            display: grid;
            grid-template-columns: repeat(8, 1fr);
            grid-template-rows: repeat(8, 1fr);
            margin: 20px auto;
            font-size: 2em;
        }
        .square {
            width: 100%;
            height: 100%;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
        }
        .white { background-color: #eee; }
        .green { background-color: #4caf50; }
        .piece {
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            position: absolute;
            width: 100%;
            height: 100%;
            font-size: 48px;
        }
        .piece.white {
            color: #fff;
        }
        .piece.black {
            color: #000;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div id="chessBoard"></div>
    </form>
    <script>
        function drawBoard(pieces) {
            let boardHtml = '';
            for (let i = 0; i < 64; i++) {
                let row = Math.floor(i / 8);
                let color = (row % 2 === 0) ? (i % 2 === 0 ? 'green' : 'white') : (i % 2 === 0 ? 'white' : 'green');
                boardHtml += `<div class='square ${color}' id='square${i}'></div>`;
            }
            $('#chessBoard').html(boardHtml);

            if (pieces) {
                pieces.forEach(piece => {
                    let squareIndex = piece.Position;
                    let pieceColorClass = piece.IsWhite ? 'white' : 'black';
                    let pieceHtml = `<div class='piece ${pieceColorClass}' id='piece${squareIndex}' data-piece-id='${piece.Position}'>${piece.Symbol}</div>`;
                    $(`#square${squareIndex}`).append(pieceHtml);
                });
            }

            $('.piece').draggable({
                revert: "invalid",
                start: function () {
                    $(this).css('z-index', 1000);
                },
                stop: function () {
                    $(this).css('z-index', '');
                }
            });

            $('.square').droppable({
                accept: '.piece',
                drop: function (event, ui) {
                    let pieceId = ui.draggable.data('piece-id');
                    let targetSquare = $(this).attr('id').replace('square', '');

                    movePiece(pieceId, targetSquare);
                }
            });
        }

        function movePiece(pieceId, targetSquare) {
            $.ajax({
                url: '/GameBoard.aspx/MovePiece',
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify({ pieceId: pieceId, targetSquare: targetSquare }),
                success: function (data) {
                    if (data.d.success) {
                        fetchBoardData();
                    } else {
                        alert('Invalid move.');
                    }
                },
                error: function () {
                    alert('Failed to move the piece.');
                }
            });
        }

        function fetchBoardData() {
            $.ajax({
                url: '/GameBoard.aspx/GetBoardState',
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (data) {
                    drawBoard(data.d.pieces);
                },
                error: function () {
                    alert('Failed to fetch board data.');
                }
            });
        }

        $(document).ready(function () {
            fetchBoardData();
        });
    </script>
</body>
</html>
