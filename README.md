# RoslynScribe

RoslynScribe is a tool for static analysis and visualization of C# code execution flows. It extracts execution paths marked with special `[ADC]` comments and visualizes them as interactive flowcharts.

## Features

*   **Static Analysis**: Parses C# solutions to extract execution flows based on annotated comments.
*   **Interactive Visualization**: A standalone HTML/JS viewer to explore complex execution trees.
*   **Drill-Down Capability**: Expand and collapse branches to focus on relevant logic.
*   **Semantic Search**: Find nodes by text or tags (e.g., "Error", "Warning"), even if they are currently hidden.
*   **Offline Support**: The visualizer runs entirely in the browser with no server requirement.

## How to Use

### 1. Annotate Code
Add comments starting with `[ADC]` (Annotated Documentation Comment) to your C# code to mark important execution steps.

```csharp
// [ADC] Validating user input
public void Validate(User user) { ... }
```

### 2. Analyze Solution
Run the `RoslynScribe` tool against your solution to generate the analysis result.

```bash
RoslynScribe.exe analyze -s "MySolution.sln" -o "result.adc.json"
```

### 3. Visualize Results
1.  Navigate to `src/RoslynScribe.Visualizer/`.
2.  Open `index.html` in any modern web browser.
3.  Click **Load Data** and select your generated `result.json` file.
4.  **Explore**:
    *   **Expand**: Click the `+` icon on nodes to reveal hidden details.
    *   **Details**: Click a node to view metadata (Class, Method, File) in the side panel.
    *   **Search**: Type in the search bar to find specific steps or tags.
    *   **Save State**: Use "Save Config" to export your current view (expanded nodes, active filters) to share with others.

### 4. Offline Setup (Optional)
For environments without internet access (cannot load Mermaid/PanZoom from CDN):
1.  Download `mermaid.min.js` (v10+) and `svg-pan-zoom.min.js`.
2.  Place them in the `src/RoslynScribe.Visualizer/` directory.
3.  The `index.html` will automatically detect and use these local files.

### 5. Development

To modify the visualizer:
1.  Install dependencies: `npm install`
2.  Edit TypeScript files in `src/RoslynScribe.Visualizer/`.
3.  Compile:
    ```bash
    npx tsc -p src/RoslynScribe.Visualizer/tsconfig.json
    ```

## Command Line Reference

### Commands

*   `analyze` - Analyze a solution and write a single `.adc.json` output.
*   `merge` - Merge multiple `.adc.json` files into one.

### analyze options

*   `-s`, `--solution` (required): Path to the `.sln` file.
*   `-p`, `--project` (repeatable): Project name(s) to include (exact match, case-insensitive).
*   `-pe`, `--project-exclude` (repeatable): Project name(s) to exclude (exact match, case-insensitive).
*   `-pec`, `--project-exclude-contains` (repeatable): Exclude projects containing this phrase (case-insensitive).
*   `-d`, `--dir` (repeatable): Directory to analyze (combined with files when both are provided).
*   `-f`, `--file` (repeatable): File path(s) to analyze (combined with directories when both are provided).
*   `-o`, `--output`: Output file path. Defaults to `<solutionName>.adc.json` in the current directory.
*   `-c`, `--config`: Optional ADC config JSON path. If omitted, an empty config is used.

### merge options

*   `-d`, `--dir`: Directory containing `.adc.json` files (top-level only).
*   `-f`, `--file` (repeatable): File path(s) to merge (requires at least two files when `-d` is not used).
*   `-o`, `--output`: Output file path. Defaults to `merge_<yyyy_MM_dd>.adc.json` in the current directory.

Notes:

*   For `merge`, `-d` and `-f` are mutually exclusive.
*   For `analyze`, `-d` and `-f` are combined (union).

### Example invocations

Analyze a solution with project filters and output file:

```bash
RoslynScribe.exe analyze -s "D:\Source\MyApp\MyApp.sln" -p "MyApp.Api" -pe "MyApp.Tests" -o "MyApp.adc.json"
```

Analyze only specific folders and files:

```bash
RoslynScribe.exe analyze -s "D:\Source\MyApp\MyApp.sln" -d "D:\Source\MyApp\src" -f "D:\Source\MyApp\tools\Seed.cs"
```

Analyze with a custom config:

```bash
RoslynScribe.exe analyze -s "D:\Source\MyApp\MyApp.sln" -c "D:\configs\adc.config.json"
```

Merge all `.adc.json` files in a directory:

```bash
RoslynScribe.exe merge -d "D:\Source\MyApp\results" -o "merged.adc.json"
```

Merge a specific set of files:

```bash
RoslynScribe.exe merge -f "A.adc.json" -f "B.adc.json" -o "merged.adc.json"
```

## Project Structure

*   `src/RoslynScribe.Domain`: Core analysis logic and domain models.
*   `src/RoslynScribe.Visualizer`: The web-based visualization tool (HTML/TS/CSS).
*   `src/RoslynScribe.Printer.Mermaid`: (Legacy) Backend-based Mermaid printer.
