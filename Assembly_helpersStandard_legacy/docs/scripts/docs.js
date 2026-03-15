// Documentation Interactivity Scripts
// Based on the Beep Controls documentation functionality

// Theme Management
function initializeTheme() {
    const savedTheme = localStorage.getItem('docs-theme') || 'light';
    setTheme(savedTheme);
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    setTheme(newTheme);
}

function setTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('docs-theme', theme);
    
    const themeIcon = document.getElementById('theme-icon');
    if (themeIcon) {
        themeIcon.className = theme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
    }
}

// Sidebar Management
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.classList.toggle('open');
    }
}

function initializeSidebar() {
    // Auto-expand submenu containing active link
    const activeLink = document.querySelector('.nav-menu a.active');
    if (activeLink) {
        const parentSubmenu = activeLink.closest('.has-submenu');
        if (parentSubmenu) {
            parentSubmenu.classList.add('open');
        }
    }
    
    // Add click handlers for submenu toggles
    const submenuToggles = document.querySelectorAll('.has-submenu > a');
    submenuToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            const parent = this.parentElement;
            parent.classList.toggle('open');
        });
    });
}

// Search Functionality
function searchDocs(query) {
    if (!query || query.length < 2) {
        clearSearchResults();
        return;
    }
    
    const searchResults = performSearch(query.toLowerCase());
    displaySearchResults(searchResults);
}

function performSearch(query) {
    const searchableElements = document.querySelectorAll('h1, h2, h3, h4, .method-item h3, .property-item h3');
    const results = [];
    
    searchableElements.forEach(element => {
        const text = element.textContent.toLowerCase();
        if (text.includes(query)) {
            results.push({
                element: element,
                text: element.textContent,
                section: getSection(element)
            });
        }
    });
    
    return results.slice(0, 10); // Limit to 10 results
}

function getSection(element) {
    const section = element.closest('.section');
    if (section) {
        const sectionTitle = section.querySelector('h2');
        return sectionTitle ? sectionTitle.textContent : 'Unknown Section';
    }
    return 'Unknown Section';
}

function displaySearchResults(results) {
    let searchResultsContainer = document.getElementById('search-results');
    
    if (!searchResultsContainer) {
        searchResultsContainer = document.createElement('div');
        searchResultsContainer.id = 'search-results';
        searchResultsContainer.className = 'search-results';
        
        const searchContainer = document.querySelector('.search-container');
        if (searchContainer) {
            searchContainer.appendChild(searchResultsContainer);
        }
    }
    
    if (results.length === 0) {
        searchResultsContainer.innerHTML = '<div class="no-results">No results found</div>';
        return;
    }
    
    const resultsList = results.map(result => `
        <div class="search-result-item" onclick="scrollToElement('${result.element.id || generateId(result.element)}')">
            <div class="result-title">${result.text}</div>
            <div class="result-section">${result.section}</div>
        </div>
    `).join('');
    
    searchResultsContainer.innerHTML = `
        <div class="search-results-header">Search Results</div>
        ${resultsList}
    `;
}

function clearSearchResults() {
    const searchResultsContainer = document.getElementById('search-results');
    if (searchResultsContainer) {
        searchResultsContainer.innerHTML = '';
    }
}

function generateId(element) {
    if (!element.id) {
        element.id = 'search-result-' + Math.random().toString(36).substr(2, 9);
    }
    return element.id;
}

function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'start' });
        
        // Highlight the element briefly
        element.style.backgroundColor = 'var(--color-brand-primary)';
        element.style.color = 'white';
        element.style.transition = 'all 0.3s ease';
        
        setTimeout(() => {
            element.style.backgroundColor = '';
            element.style.color = '';
        }, 2000);
    }
    
    // Clear search results
    clearSearchResults();
}

// Code Copy Functionality
function copyCode(button) {
    const codeBlock = button.closest('.code-example').querySelector('code');
    if (!codeBlock) return;
    
    const text = codeBlock.textContent;
    
    // Use modern Clipboard API if available
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            showCopyFeedback(button, 'Copied!');
        }).catch(() => {
            fallbackCopyTextToClipboard(text, button);
        });
    } else {
        fallbackCopyTextToClipboard(text, button);
    }
}

function fallbackCopyTextToClipboard(text, button) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        document.execCommand('copy');
        showCopyFeedback(button, 'Copied!');
    } catch (err) {
        showCopyFeedback(button, 'Copy failed');
    }
    
    document.body.removeChild(textArea);
}

function showCopyFeedback(button, message) {
    const originalText = button.innerHTML;
    button.innerHTML = `<i class="bi bi-check"></i> ${message}`;
    button.style.background = 'var(--color-brand-primary)';
    button.style.color = 'white';
    
    setTimeout(() => {
        button.innerHTML = originalText;
        button.style.background = '';
        button.style.color = '';
    }, 2000);
}

// Smooth Scrolling for Anchor Links
function initializeSmoothScrolling() {
    const anchorLinks = document.querySelectorAll('a[href^="#"]');
    
    anchorLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
                
                // Update URL without jumping
                history.pushState(null, null, `#${targetId}`);
            }
        });
    });
}

// Code Syntax Highlighting Enhancement
function enhanceCodeBlocks() {
    const codeBlocks = document.querySelectorAll('pre code');
    
    codeBlocks.forEach(block => {
        // Add line numbers if needed
        if (block.textContent.split('\n').length > 5) {
            addLineNumbers(block);
        }
        
        // Enhance C# syntax highlighting
        if (block.classList.contains('language-csharp') || 
            block.closest('.code-example').querySelector('.language')?.textContent === 'C#') {
            enhanceCSharpSyntax(block);
        }
    });
}

function addLineNumbers(codeBlock) {
    const lines = codeBlock.textContent.split('\n');
    const lineNumbers = lines.map((_, index) => index + 1).join('\n');
    
    const lineNumbersElement = document.createElement('div');
    lineNumbersElement.className = 'line-numbers';
    lineNumbersElement.style.cssText = `
        position: absolute;
        left: 0;
        top: 0;
        padding: 1rem 0.5rem;
        background: var(--color-background-secondary);
        color: var(--color-foreground-muted);
        font-family: inherit;
        font-size: inherit;
        line-height: inherit;
        border-right: 1px solid var(--color-background-border);
        user-select: none;
        white-space: pre;
    `;
    lineNumbersElement.textContent = lineNumbers;
    
    const pre = codeBlock.closest('pre');
    pre.style.position = 'relative';
    pre.style.paddingLeft = '3rem';
    pre.insertBefore(lineNumbersElement, codeBlock);
}

function enhanceCSharpSyntax(codeBlock) {
    // Additional C# keyword highlighting beyond Prism.js
    const keywords = [
        'var', 'dynamic', 'async', 'await', 'yield', 'nameof', 'typeof',
        'Assembly', 'Type', 'IProgress', 'CancellationToken', 'Task'
    ];
    
    let html = codeBlock.innerHTML;
    
    keywords.forEach(keyword => {
        const regex = new RegExp(`\\b${keyword}\\b`, 'g');
        html = html.replace(regex, `<span class="token keyword">${keyword}</span>`);
    });
    
    codeBlock.innerHTML = html;
}

// Table Enhancement
function enhanceTables() {
    const tables = document.querySelectorAll('table');
    
    tables.forEach(table => {
        // Make tables responsive
        const wrapper = document.createElement('div');
        wrapper.className = 'table-wrapper';
        wrapper.style.cssText = 'overflow-x: auto; margin: 1rem 0;';
        
        table.parentNode.insertBefore(wrapper, table);
        wrapper.appendChild(table);
        
        // Add sorting capability for data tables
        if (table.classList.contains('params-table') || table.classList.contains('property-table')) {
            addTableSorting(table);
        }
    });
}

function addTableSorting(table) {
    const headers = table.querySelectorAll('th');
    
    headers.forEach((header, index) => {
        header.style.cursor = 'pointer';
        header.addEventListener('click', () => sortTable(table, index));
        
        // Add sort indicator
        const sortIndicator = document.createElement('span');
        sortIndicator.className = 'sort-indicator';
        sortIndicator.innerHTML = ' <i class="bi bi-arrow-down-up"></i>';
        sortIndicator.style.opacity = '0.5';
        header.appendChild(sortIndicator);
    });
}

function sortTable(table, columnIndex) {
    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr'));
    
    const isAscending = table.getAttribute('data-sort-direction') !== 'asc';
    
    rows.sort((a, b) => {
        const aText = a.cells[columnIndex].textContent.trim();
        const bText = b.cells[columnIndex].textContent.trim();
        
        return isAscending ? 
            aText.localeCompare(bText, undefined, { numeric: true }) :
            bText.localeCompare(aText, undefined, { numeric: true });
    });
    
    rows.forEach(row => tbody.appendChild(row));
    
    table.setAttribute('data-sort-direction', isAscending ? 'asc' : 'desc');
    
    // Update sort indicators
    const indicators = table.querySelectorAll('.sort-indicator i');
    indicators.forEach((indicator, index) => {
        if (index === columnIndex) {
            indicator.className = isAscending ? 'bi bi-arrow-up' : 'bi bi-arrow-down';
        } else {
            indicator.className = 'bi bi-arrow-down-up';
        }
    });
}

// Section Navigation
function initializeSectionNavigation() {
    const sections = document.querySelectorAll('.section');
    
    // Create floating table of contents for long pages
    if (sections.length > 5) {
        createFloatingTOC(sections);
    }
    
    // Highlight current section in navigation
    window.addEventListener('scroll', () => {
        highlightCurrentSection(sections);
    });
}

function createFloatingTOC(sections) {
    const toc = document.createElement('div');
    toc.id = 'floating-toc';
    toc.style.cssText = `
        position: fixed;
        right: 2rem;
        top: 50%;
        transform: translateY(-50%);
        background: var(--color-background-secondary);
        border: 1px solid var(--color-background-border);
        border-radius: var(--border-radius);
        padding: 1rem;
        max-height: 50vh;
        overflow-y: auto;
        z-index: 1000;
        opacity: 0.9;
        transition: opacity 0.2s ease;
        display: none;
    `;
    
    const tocTitle = document.createElement('h4');
    tocTitle.textContent = 'On this page';
    tocTitle.style.margin = '0 0 0.5rem 0';
    tocTitle.style.fontSize = '0.875rem';
    toc.appendChild(tocTitle);
    
    const tocList = document.createElement('ul');
    tocList.style.cssText = 'list-style: none; margin: 0; padding: 0;';
    
    sections.forEach(section => {
        const heading = section.querySelector('h2, h3');
        if (heading) {
            const li = document.createElement('li');
            const a = document.createElement('a');
            a.href = `#${heading.id || generateId(heading)}`;
            a.textContent = heading.textContent;
            a.style.cssText = `
                display: block;
                padding: 0.25rem 0;
                color: var(--color-link);
                font-size: 0.75rem;
                text-decoration: none;
            `;
            li.appendChild(a);
            tocList.appendChild(li);
        }
    });
    
    toc.appendChild(tocList);
    document.body.appendChild(toc);
    
    // Show/hide based on scroll position
    window.addEventListener('scroll', () => {
        const shouldShow = window.scrollY > 300 && window.innerWidth > 1200;
        toc.style.display = shouldShow ? 'block' : 'none';
    });
}

function highlightCurrentSection(sections) {
    const scrollY = window.scrollY + 100; // Offset for better detection
    
    sections.forEach(section => {
        const top = section.offsetTop;
        const height = section.offsetHeight;
        
        if (scrollY >= top && scrollY < top + height) {
            // Could highlight corresponding nav item here
        }
    });
}

// Accessibility Enhancements
function enhanceAccessibility() {
    // Add skip links
    const skipLink = document.createElement('a');
    skipLink.href = '#main-content';
    skipLink.textContent = 'Skip to main content';
    skipLink.style.cssText = `
        position: absolute;
        top: -40px;
        left: 6px;
        background: var(--color-brand-primary);
        color: white;
        padding: 8px;
        text-decoration: none;
        z-index: 1000;
        border-radius: 0 0 4px 4px;
    `;
    skipLink.addEventListener('focus', () => {
        skipLink.style.top = '0';
    });
    skipLink.addEventListener('blur', () => {
        skipLink.style.top = '-40px';
    });
    
    document.body.insertBefore(skipLink, document.body.firstChild);
    
    // Add main content ID
    const mainContent = document.querySelector('.content');
    if (mainContent) {
        mainContent.id = 'main-content';
    }
    
    // Enhance focus indicators
    const focusableElements = document.querySelectorAll('a, button, input, [tabindex]');
    focusableElements.forEach(element => {
        element.addEventListener('focus', function() {
            this.style.outline = '2px solid var(--color-brand-primary)';
            this.style.outlineOffset = '2px';
        });
        element.addEventListener('blur', function() {
            this.style.outline = '';
            this.style.outlineOffset = '';
        });
    });
}

// Performance Optimization
function optimizePerformance() {
    // Lazy load images
    const images = document.querySelectorAll('img');
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                        imageObserver.unobserve(img);
                    }
                }
            });
        });
        
        images.forEach(img => {
            if (img.dataset.src) {
                imageObserver.observe(img);
            }
        });
    }
    
    // Debounce search
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                searchDocs(this.value);
            }, 300);
        });
    }
}

// Initialize everything when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeTheme();
    initializeSidebar();
    initializeSmoothScrolling();
    enhanceCodeBlocks();
    enhanceTables();
    initializeSectionNavigation();
    enhanceAccessibility();
    optimizePerformance();
    
    // Close search results when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.search-container')) {
            clearSearchResults();
        }
    });
    
    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Ctrl/Cmd + K to focus search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.focus();
            }
        }
        
        // Escape to clear search
        if (e.key === 'Escape') {
            clearSearchResults();
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.value = '';
                searchInput.blur();
            }
        }
    });
    
    console.log('Assembly Helpers Documentation loaded successfully');
});