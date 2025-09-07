/**
 * Beep Data Management Engine Documentation - Navigation Manager
 * Handles dynamic navigation loading and active state management
 */

class NavigationManager {
    constructor() {
        this.currentPage = this.getCurrentPageName();
        this.navigationMapping = this.createNavigationMapping();
    }

    getCurrentPageName() {
        const path = window.location.pathname;
        const filename = path.split('/').pop() || 'index.html';
        return filename.replace('.html', '');
    }

    createNavigationMapping() {
        return {
            // Home
            'index': { 
                activeId: 'nav-home', 
                openSection: null 
            },
            
            // Getting Started
            'registerbeep': { 
                activeId: 'nav-registration', 
                openSection: 'nav-getting-started' 
            },
            'unitofwork': { 
                activeId: 'nav-tutorial', 
                openSection: 'nav-getting-started' 
            },
            'data-management-examples': { 
                activeId: 'nav-data-sources', 
                openSection: 'nav-getting-started' 
            },
            
            // Core Concepts
            'unitofwork-deep-dive': { 
                activeId: 'nav-unitofwork-deep', 
                openSection: 'nav-core-concepts' 
            },
            'multidatasource-unitofwork': { 
                activeId: 'nav-multidatasource', 
                openSection: 'nav-core-concepts' 
            },
            
            // Data Management
            'examples': { 
                activeId: 'nav-examples', 
                openSection: 'nav-data-management' 
            },
            'webapidatasource': { 
                activeId: 'nav-webapi', 
                openSection: 'nav-data-management' 
            },
            'best-practices': { 
                activeId: 'nav-best-practices', 
                openSection: 'nav-data-management' 
            },
            
            // Editor Classes
            'dmeeditor': { 
                activeId: 'nav-dmeeditor', 
                openSection: 'nav-editor-classes' 
            },
            'etleditor': { 
                activeId: 'nav-etleditor', 
                openSection: 'nav-editor-classes' 
            },
            'dataimportmanager': { 
                activeId: 'nav-dataimport', 
                openSection: 'nav-editor-classes' 
            },
            'mappingmanager': { 
                activeId: 'nav-mapping', 
                openSection: 'nav-editor-classes' 
            },
            'defaultsmanager': { 
                activeId: 'nav-defaults', 
                openSection: 'nav-editor-classes' 
            },
            'datasyncmanager': { 
                activeId: 'nav-datasync', 
                openSection: 'nav-editor-classes' 
            },
            'configeditor': { 
                activeId: 'nav-configeditor', 
                openSection: 'nav-editor-classes' 
            },
            
            // Advanced Topics
            'etl-workflow-engine': { 
                activeId: 'nav-etl-workflow', 
                openSection: 'nav-advanced-topics' 
            },
            'advanced-tools-utilities': { 
                activeId: 'nav-advanced-tools', 
                openSection: 'nav-advanced-topics' 
            },
            'creating-custom-datasources': { 
                activeId: 'nav-custom-datasources', 
                openSection: 'nav-advanced-topics' 
            },
            'api-reference': { 
                activeId: 'nav-api-reference', 
                openSection: 'nav-advanced-topics' 
            }
        };
    }

    getNavigationHTML() {
        return `
        <!-- Beep Data Management Engine - Shared Navigation Component -->
        <div class="logo">
            <img src="assets/beep-logo.svg" alt="Beep Data Management Engine Logo">
            <div class="logo-text">
                <h2>Beep DME</h2>
                <span class="version">v2.0.32</span>
            </div>
        </div>

        <!-- Search -->
        <div class="search-container">
            <input type="text" class="search-input" placeholder="Search documentation..." onkeyup="searchDocs(this.value)">
        </div>

        <nav>
            <ul class="nav-menu">
                <li><a href="index.html" id="nav-home"><i class="bi bi-house"></i> Home</a></li>
                <li class="has-submenu" id="nav-getting-started">
                    <a href="#"><i class="bi bi-rocket"></i> Getting Started</a>
                    <ul class="submenu">
                        <li><a href="registerbeep.html" id="nav-registration">Registration & Setup</a></li>
                        <li><a href="unitofwork.html" id="nav-tutorial">Quick Start Tutorial</a></li>
                        <li><a href="data-management-examples.html" id="nav-data-sources">Data Sources</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-core-concepts">
                    <a href="#"><i class="bi bi-layers"></i> Core Concepts</a>
                    <ul class="submenu">
                        <li><a href="unitofwork.html" id="nav-unitofwork">UnitOfWork Pattern</a></li>
                        <li><a href="unitofwork-deep-dive.html" id="nav-unitofwork-deep">UnitOfWork Deep Dive</a></li>
                        <li><a href="multidatasource-unitofwork.html" id="nav-multidatasource">Multi-DataSource UnitOfWork</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-data-management">
                    <a href="#"><i class="bi bi-database"></i> Data Management</a>
                    <ul class="submenu">
                        <li><a href="data-management-examples.html" id="nav-data-examples">Data Sources</a></li>
                        <li><a href="examples.html" id="nav-examples">Code Examples</a></li>
                        <li><a href="best-practices.html" id="nav-best-practices">Best Practices</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-editor-classes">
                    <a href="#"><i class="bi bi-gear"></i> Editor Classes</a>
                    <ul class="submenu">
                        <li><a href="dmeeditor.html" id="nav-dmeeditor">DMEEditor</a></li>
                        <li><a href="etleditor.html" id="nav-etleditor">ETL Editor</a></li>
                        <li><a href="dataimportmanager.html" id="nav-dataimport">Data Import Manager</a></li>
                        <li><a href="mappingmanager.html" id="nav-mapping">Mapping Manager</a></li>
                        <li><a href="defaultsmanager.html" id="nav-defaults">Defaults Manager</a></li>
                        <li><a href="datasyncmanager.html" id="nav-datasync">Data Sync Manager</a></li>
                        <li><a href="configeditor.html" id="nav-configeditor">ConfigEditor</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-advanced-topics">
                    <a href="#"><i class="bi bi-tools"></i> Advanced Topics</a>
                    <ul class="submenu">
                        <li><a href="etl-workflow-engine.html" id="nav-etl-workflow">ETL & Workflow Engine</a></li>
                        <li><a href="advanced-tools-utilities.html" id="nav-advanced-tools">Advanced Tools & Utilities</a></li>
                        <li><a href="creating-custom-datasources.html" id="nav-custom-datasources">Creating Custom Data Sources</a></li>
                        <li><a href="api-reference.html" id="nav-api-reference">API Reference</a></li>
                    </ul>
                </li>
                <li><a href="api-reference.html" id="nav-api"><i class="bi bi-code-square"></i> API Reference</a></li>
            </ul>
        </nav>
        `;
    }

    async loadNavigation() {
        try {
            // Get navigation HTML from embedded function (no fetch needed)
            const navigationHtml = this.getNavigationHTML();
            
            // Insert navigation into sidebar
            const sidebar = document.getElementById('sidebar');
            if (sidebar) {
                sidebar.innerHTML = navigationHtml;
                
                // Set up navigation after loading
                this.setupNavigation();
                console.log('? Navigation loaded successfully');
            } else {
                console.error('? Sidebar element not found');
            }
        } catch (error) {
            console.error('? Error loading navigation:', error);
            // Fallback: Show error message in sidebar
            const sidebar = document.getElementById('sidebar');
            if (sidebar) {
                sidebar.innerHTML = `
                    <div class="navigation-error">
                        <h3>Navigation Error</h3>
                        <p>Failed to load navigation. Please refresh the page.</p>
                        <p>Error: ${error.message}</p>
                    </div>
                `;
            }
        }
    }

    setupNavigation() {
        // Set active states based on current page
        this.setActiveStates();
        
        // Set up submenu toggles
        this.setupSubmenuToggles();
        
        // Set up search functionality
        this.setupSearch();
        
        // Set up theme toggle (if not already set up)
        this.setupThemeToggle();
    }

    setActiveStates() {
        const mapping = this.navigationMapping[this.currentPage];
        
        if (mapping) {
            // Set active link
            if (mapping.activeId) {
                const activeElement = document.getElementById(mapping.activeId);
                if (activeElement) {
                    activeElement.classList.add('active');
                }
            }
            
            // Open parent section
            if (mapping.openSection) {
                const sectionElement = document.getElementById(mapping.openSection);
                if (sectionElement) {
                    sectionElement.classList.add('open');
                }
            }
        }
        
        console.log(`?? Set active state for page: ${this.currentPage}`);
    }

    setupSubmenuToggles() {
        const submenus = document.querySelectorAll('.has-submenu > a');
        
        submenus.forEach(item => {
            item.addEventListener('click', function(e) {
                e.preventDefault();
                const parent = this.parentElement;
                parent.classList.toggle('open');
            });
        });
    }

    setupSearch() {
        // Search functionality is handled by the global searchDocs function
        // This is just a placeholder for additional search features
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.addEventListener('input', function(e) {
                if (typeof searchDocs === 'function') {
                    searchDocs(e.target.value);
                }
            });
        }
    }

    setupThemeToggle() {
        // Theme toggle functionality is handled globally
        // Load saved theme
        const savedTheme = localStorage.getItem('theme');
        if (savedTheme === 'dark') {
            document.body.setAttribute('data-theme', 'dark');
            const themeIcon = document.getElementById('theme-icon');
            if (themeIcon) {
                themeIcon.className = 'bi bi-moon-fill';
            }
        }
    }
}

// Global navigation manager instance
let navigationManager;

// Initialize navigation when DOM is loaded
document.addEventListener('DOMContentLoaded', async function() {
    console.log('?? Initializing Beep DME Documentation navigation...');
    
    try {
        // Create navigation manager
        navigationManager = new NavigationManager();
        
        // Load navigation
        await navigationManager.loadNavigation();
        
        console.log('? Beep DME Documentation navigation initialized successfully');
    } catch (error) {
        console.error('? Failed to initialize navigation:', error);
    }
});

// Global theme toggle function (called by theme toggle button)
function toggleTheme() {
    const body = document.body;
    const themeIcon = document.getElementById('theme-icon');
    const currentTheme = body.getAttribute('data-theme');
    
    if (currentTheme === 'dark') {
        body.removeAttribute('data-theme');
        if (themeIcon) themeIcon.className = 'bi bi-sun-fill';
        localStorage.setItem('theme', 'light');
    } else {
        body.setAttribute('data-theme', 'dark');
        if (themeIcon) themeIcon.className = 'bi bi-moon-fill';
        localStorage.setItem('theme', 'dark');
    }
}

// Global sidebar toggle function (called by mobile menu button)
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.classList.toggle('open');
    }
}

// Global search function (called by search input)
function searchDocs(query = '') {
    const links = document.querySelectorAll('.nav-menu a');
    const lowerQuery = query.toLowerCase();
    
    links.forEach(link => {
        const text = link.textContent.toLowerCase();
        const listItem = link.closest('li');
        
        if (text.includes(lowerQuery) || lowerQuery === '') {
            if (listItem) listItem.style.display = '';
        } else {
            if (listItem) listItem.style.display = 'none';
        }
    });
}

// Global function for API section toggles (used in some pages)
function toggleSection(header) {
    const content = header.nextElementSibling;
    const icon = header.querySelector('.toggle-icon');
    
    if (content && content.classList.contains('active')) {
        content.classList.remove('active');
        if (icon) icon.style.transform = 'rotate(0deg)';
    } else {
        if (content) content.classList.add('active');
        if (icon) icon.style.transform = 'rotate(90deg)';
    }
}

// Export for use in modules (if needed)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { NavigationManager, toggleTheme, toggleSidebar, searchDocs, toggleSection };
}