using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Create a Flow-feild as described in GameAiPro witch is based on Dijkstra’s shortest path algorithm (Greedy Algo-7)
 * Alternatively we could use A* However iv used A* befor.
 * 
 * --
 * 
 * Befor we create the flow-feild we need to find all cells what contain objects that 
 * require avoiding. (For this we will just use units world cords for our grid)
 * 
 * --
 * 
 * Simular to A* in Dijkstra algorithm uses an open (un explored) and closed (explored) list
 * When cells are discoved they are added to the open list with a value of Mathf.Infinity, except 
 * for the source witch has a value of 0.
 * 
 * 
 * 
 * -------------------------------------------------
 * Resources
 * -------------------------------------------------
 *  
 * Gane AI Pro - Efficient Crowd Simulation For Mobile Games, By Graham
 * Dijkstra’s shortest path algorithm - https://www.geeksforgeeks.org/dijkstras-shortest-path-algorithm-greedy-algo-7/
 * 
 */

public class FlowFeild : MonoBehaviour
{

    [Header("Area")]
    // The Area to genarate the flow-feild for from in world position
    [SerializeField] private Vector2Int area = new Vector2Int( 50, 20 );
    [SerializeField] private Vector2Int[] sourceCells; // from top left.

    [Header( "Object Detection" )]
    [SerializeField, Range(0.1f, 1f)] 
    private float rayCellSize = 0.9f;
    private RaycastHit2D[] rayHit = new RaycastHit2D[1];    // we only care if we hit one object.
    [SerializeField]
    private LayerMask detectObjectsMask;

    private CellScore[] flowFeild;


    void Awake()
    {
        GenerateFlowFeild( sourceCells );
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GenerateFlowFeild( Vector2Int[] startCells )
    {

        bool[] explored = new bool[ area.x * area.y ];
        CellScore[] cells = new CellScore[ area.x * area.y ];

        // init all cells
        for ( int y = 0; y < area.y; y++ )
        {
            for ( int x = 0; x < area.x; x++ )
            {

                int id = y * area.x + x;
                cells[ id ] = new CellScore();

                // find if theres an object in the cell.
                int hits = Physics2D.BoxCastNonAlloc( new Vector2( x, y ), Vector2.one * rayCellSize, 0, Vector2.zero, rayHit, 0, detectObjectsMask );

                if ( hits > 0 )
                {
                    cells[ id ].blocked = true;
                    explored[ id ] = true;
                    continue;
                }
                
                explored[ id ] = false;

                // Find if this is a start cell, assiging it a score of 0 if it is
                // Otherwise assign the max score
                bool isStartPoint = false;

                for ( int i = 0; i < startCells.Length; i++ )
                    if ( startCells[i].x == x && startCells[i].y == y )
                    {
                        cells[id].Init( new Vector2( x, y ), 0 );
                        isStartPoint = true;
                    }


                if ( !isStartPoint )
                    cells[id].Init( new Vector2( x, y ), int.MaxValue );


            }
        }

        // explor all cells and find the shortest paths
        // Find the lowest scoring unexplored cell,
        // and update the ajcent cells score/distance witch is lower than the current score..

        (int lowestId, CellScore cell) lowestCellCell = GetLowestUnexploredCell( explored, cells );

        CellScore currentCell = lowestCellCell.cell;
        int currentId = lowestCellCell.lowestId;

        while( currentCell != null )
        {

            // get the ajcent cells
            for ( int x = -1; x < 2; x++ )
            {

                if ( currentCell.position.x + x < 0 || currentCell.position.x + x >= area.x )
                    continue;

                for ( int y = -1; y < 2; y++ )
                {

                    if ( (x == 0 && y == 0) || currentCell.position.y + y < 0 || currentCell.position.y + y >= area.y )
                        continue;

                    int cellId = ((int)currentCell.position.y + y) * area.x + ((int)currentCell.position.x + x);

                    if ( cells[cellId].blocked )
                        continue;

                    float ajcentCellScore = currentCell.score + ( (x == 0 || y == 0) ? 1f : 1.5f );

                    if ( ajcentCellScore < cells[cellId].score )
                        cells[cellId].Update( currentCell.position, ajcentCellScore );

                }
            }

            explored[ currentId ] = true;

            lowestCellCell = GetLowestUnexploredCell( explored, cells );

            currentCell = lowestCellCell.cell;
            currentId = lowestCellCell.lowestId;
        }

        flowFeild = cells;

        print( "Generated Flowfeild." );

        string s = "";

        // Draw the flow feild.
        for ( int i = 0; i < cells.Length; i++ )
        {

            if ( i % area.x == 0 )
                s += "\n";

            s += $"|{cells[i].score}|";

            Vector3 startPos = cells[i].position;
            Vector3 endPos = cells[i].position + (cells[i].direction / 2f);

            Debug.DrawLine( startPos, endPos, Color.red, 600 );

        }

        print( s );
	}

    (int, CellScore) GetLowestUnexploredCell( bool[] explored, CellScore[] cells )
    {
        CellScore lowest = null;
        int lowestId = -1;

        for ( int i = 0; i < explored.Length; i++ )
        {
            if ( explored[i] )
                continue;

            if ( lowest == null )
            {
                lowest = cells[i];
                lowestId = i;
            }
            else if ( cells[i].score < lowest.score )
            {
                lowest = cells[i];
                lowestId = i;
            }
        }

         
        return (lowestId, lowest);
	}

    private Vector3 RoundToFlowGrid( Vector3 worldPosition )
    {

        // round the world position to the nearest cell. 
        // the center of the cell is at .0
        if ( worldPosition.x % 1f >= 0.5f )
            worldPosition.x = Mathf.Ceil( worldPosition.x );
        else
            worldPosition.x = Mathf.Floor( worldPosition.x );

        if ( worldPosition.y % 1f >= 0.5f )
            worldPosition.y = Mathf.Ceil( worldPosition.y );
        else
            worldPosition.y = Mathf.Floor( worldPosition.y );

        return worldPosition;
    }

    /// <summary>
    /// Returns true if agents cell is not blocked by an obstacle
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="directionVector"></param>
    /// <returns></returns>
    public bool GetFlowFeildDirectionVector( Vector3 worldPosition, out Vector2 directionVector )
    {

        Vector3 wp = worldPosition;

        worldPosition = RoundToFlowGrid( worldPosition );

        int cellId = (int)worldPosition.y * area.x + (int)worldPosition.x;

        if ( cellId < 0 || cellId >= flowFeild.Length )
        {

            directionVector = Vector2.zero;
            return false;
		}

        // Find the vector between the players position and the direction vector.
        Vector3 directionPosition = flowFeild[cellId].position + flowFeild[cellId].direction * 5;

        directionVector = (directionPosition - wp).normalized; //*/ flowFeild[cellId].direction;

        //print( $"{flowFeild[cellId].direction} -> {directionVector}" );

        if ( CSAgent.DEBUG_DRAW_ALL )
        {
            Debug.DrawLine( flowFeild[cellId].position + new Vector2( -0.5f, 0f ), flowFeild[cellId].position + new Vector2( 0.5f, 0f ), Color.white, Time.deltaTime );

            Debug.DrawLine( flowFeild[cellId].position, flowFeild[cellId].position + flowFeild[cellId].direction, Color.green, Time.deltaTime );
            Debug.DrawLine( wp, flowFeild[cellId].position + flowFeild[cellId].direction, Color.green, Time.deltaTime );
        }

        return !flowFeild[cellId].blocked;

    }

    public bool WorldPositionIsSourceCell( Vector3 worldPosition )
    {
        worldPosition = RoundToFlowGrid( worldPosition );

        foreach ( Vector2Int startCell in sourceCells )
            if ( worldPosition.x == startCell.x && worldPosition.y == startCell.y )
                return false;

        return false;

	}

    public bool InRangeOfSourceCell( Vector3 worldPosition, float distance=2f )
    {

        foreach ( Vector2Int startCell in sourceCells )
            if ( Vector3.Distance( new Vector3( startCell.x, startCell.y ), worldPosition ) <= distance)
                return true;

        return false;
	}

    /// <summary>
    /// Gets a rondom cell that is not blocked.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetRandomCell()
    {

        CellScore cell;

        do
        {
            cell = flowFeild[Random.Range( 0, flowFeild.Length )];
        } while ( cell.blocked );

        return cell.position;

	}

    class CellScore
    {

        public bool blocked = false;
        public Vector2 position;
        public Vector2 cloestCell;
        public float score;
        public Vector2 direction;

        public void Init( Vector2 pos, int dist )
        {
            position = pos;
            score = dist;
		}

        public void Update( Vector2 closeCell, float s )
        {
            
            cloestCell = closeCell;
            score = s;
            direction = ( cloestCell - position ).normalized;

		}

	}

	// Debuging
	private void OnDrawGizmos()
	{
        Gizmos.color = new Color( 0, 0, 0, 0.25f );
        Gizmos.DrawCube( new Vector3( area.x/2f-0.5f, area.y/2f-0.5f, 0f ), new Vector3( area.x, area.y, 0.1f) );

        
        Vector3 startPos = new Vector3(0, 0);

        for ( int x = 0; x < area.x; x++ )
            for ( int y = 0; y < area.y; y++ )
            {

                int cellID = y * area.x + x;

                if ( flowFeild != null && cellID < flowFeild.Length && flowFeild[cellID].blocked )
                    continue;

                startPos.x = x;
                startPos.y = y;
                                        
                Gizmos.color = new Color( 0, 0, 0, 0.5f );

                // Only draw the first top and left line

                if ( y == 0 )
                    Gizmos.DrawLine( startPos + new Vector3( -0.5f, 0.5f ), startPos + new Vector3( 0.5f, 0.5f ) ); // top

                if ( x == 0 )
                    Gizmos.DrawLine( startPos + new Vector3( -0.5f, 0.5f ), startPos + new Vector3( -0.5f, -0.5f ) ); // left

                Gizmos.DrawLine( startPos + new Vector3( -0.5f, -0.5f ), startPos + new Vector3( 0.5f, -0.5f ) );  // bottom
                Gizmos.DrawLine( startPos + new Vector3( 0.5f, 0.5f ), startPos + new Vector3( 0.5f, -0.5f ) );  // right

                foreach ( Vector2Int sourceCell in sourceCells )
                    if ( x == sourceCell.x && y == sourceCell.y )
                    {
                        Gizmos.color = new Color( 0, 1, 0, 0.5f );
                        Gizmos.DrawCube( startPos, Vector3.one * 0.5f );
				    }

            }
    }

}
