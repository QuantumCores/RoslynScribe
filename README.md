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
RoslynScribe.exe --solution "MySolution.sln" --output "result.json"
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

## Project Structure

*   `src/RoslynScribe.Domain`: Core analysis logic and domain models.
*   `src/RoslynScribe.Visualizer`: The web-based visualization tool (HTML/TS/CSS).
*   `src/RoslynScribe.Printer.Mermaid`: (Legacy) Backend-based Mermaid printer.
