# Assembly Helpers Documentation

Professional-grade HTML documentation for the TheTechIdea.Beep.Tools.AssemblyHandler library.

## Overview

This documentation provides comprehensive coverage of the Assembly Helpers library, a sophisticated .NET library for dynamic assembly loading, extension scanning, and driver management in .NET 8 and 9 applications.

## Features Documented

- **AssemblyHandler Class** - Complete API reference with examples
- **Assembly Loading** - Dynamic loading with caching and error handling
- **Extension Scanning** - Plugin discovery and instantiation
- **Driver Management** - Database driver discovery and configuration
- **Performance Optimization** - Type caching and parallel processing
- **Error Handling** - Robust error management patterns

## Documentation Structure

```
docs/
??? index.html                    # Main documentation index
??? sphinx-style.css             # Professional styling (Sphinx/Furo inspired)
??? scripts/
?   ??? docs.js                  # Interactive features
??? classes/
?   ??? assembly-handler.html    # AssemblyHandler API reference
??? guides/
?   ??? getting-started.html     # Getting started guide
??? extensions/                  # Extension documentation (planned)
??? drivers/                     # Driver documentation (planned)
??? api/                        # Additional API references (planned)
```

## Key Features

### Professional Styling
- **Sphinx/Furo inspired design** - Modern, clean documentation layout
- **Dark/Light themes** - Automatic theme switching with persistence
- **Responsive design** - Works on desktop, tablet, and mobile
- **Syntax highlighting** - Code examples with copy functionality

### Interactive Features
- **Live search** - Real-time documentation search
- **Code copying** - One-click code example copying
- **Smooth navigation** - Animated scrolling and transitions
- **Collapsible sidebar** - Space-efficient navigation

### Comprehensive Content
- **Complete API coverage** - All public methods and properties documented
- **Real-world examples** - Practical code examples for every feature
- **Best practices** - Performance tips and usage patterns
- **Error handling** - Comprehensive error management guidance

## Getting Started

1. **Open the documentation**: Start with `index.html` in your web browser
2. **Explore the AssemblyHandler**: Check out `classes/assembly-handler.html` for the complete API
3. **Follow the guide**: Use `guides/getting-started.html` for step-by-step implementation
4. **Copy examples**: Use the copy buttons to quickly use code examples in your project

## Documentation Standards

This documentation follows professional standards similar to:
- **Sphinx/Furo theme** for .NET documentation
- **Microsoft Docs** styling patterns
- **MDN Web Docs** interaction patterns
- **GitHub Documentation** navigation structure

## Browser Support

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- Mobile browsers (responsive design)

## Technologies Used

- **HTML5** - Semantic markup
- **CSS3** - Modern styling with CSS custom properties
- **JavaScript ES6+** - Interactive features
- **Prism.js** - Syntax highlighting
- **Bootstrap Icons** - Professional iconography

## Performance Features

- **Lazy loading** - Images and content loaded on demand
- **Cached themes** - User preference persistence
- **Optimized search** - Debounced search with result caching
- **Responsive images** - Optimized for different screen sizes

## Development

### File Structure
- `index.html` - Main entry point with overview and features
- `classes/assembly-handler.html` - Complete AssemblyHandler API documentation
- `guides/getting-started.html` - Step-by-step tutorial
- `sphinx-style.css` - Professional styling system
- `scripts/docs.js` - Interactive functionality

### Styling System
The CSS uses CSS custom properties for theming:
```css
:root {
    --color-brand-primary: #2962ff;
    --color-background-primary: #fff;
    --color-foreground-primary: #000;
    /* ... */
}

[data-theme="dark"] {
    --color-background-primary: #131416;
    --color-foreground-primary: #ffffffcc;
    /* ... */
}
```

### JavaScript Features
- Theme management with localStorage persistence
- Real-time search with highlighting
- Code copy functionality with feedback
- Responsive sidebar management
- Smooth scrolling navigation

## Future Enhancements

1. **Additional Pages**:
   - Assembly loading advanced patterns
   - Extension scanning deep dive
   - Driver management guide
   - Performance optimization guide

2. **Interactive Features**:
   - Code playground integration
   - API explorer
   - Interactive examples
   - Video tutorials

3. **Content Expansion**:
   - More code examples
   - Troubleshooting guide
   - FAQ section
   - Migration guides

## Contributing

To contribute to this documentation:

1. Follow the existing HTML structure and CSS patterns
2. Use semantic HTML5 elements
3. Maintain consistent code example formatting
4. Test on multiple browsers and screen sizes
5. Ensure accessibility standards are met

## License

This documentation is part of the TheTechIdea.Beep.Tools.AssemblyHandler library and follows the same licensing terms.

---

**Built with ?? for the .NET community**