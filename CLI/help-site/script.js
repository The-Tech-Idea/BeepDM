// Smooth scrolling for navigation links
document.querySelectorAll('.nav-menu a').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        
        // Remove active class from all links
        document.querySelectorAll('.nav-menu a').forEach(link => {
            link.classList.remove('active');
        });
        
        // Add active class to clicked link
        this.classList.add('active');
        
        // Get the target section
        const href = this.getAttribute('href');
        const targetSection = document.querySelector(href);
        
        if (targetSection) {
            targetSection.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Update active link on scroll
window.addEventListener('scroll', () => {
    let current = '';
    const sections = document.querySelectorAll('.doc-section');
    
    sections.forEach(section => {
        const sectionTop = section.offsetTop;
        const sectionHeight = section.clientHeight;
        
        if (window.pageYOffset >= sectionTop - 100) {
            current = section.getAttribute('id');
        }
    });
    
    document.querySelectorAll('.nav-menu a').forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('href') === `#${current}`) {
            link.classList.add('active');
        }
    });
});

// Search functionality
function searchCommands() {
    const searchInput = document.getElementById('searchInput');
    const filter = searchInput.value.toLowerCase();
    const commandCards = document.querySelectorAll('.command-card');
    const sections = document.querySelectorAll('.doc-section');
    
    if (filter === '') {
        // Show all commands and sections
        commandCards.forEach(card => {
            card.style.display = 'block';
        });
        sections.forEach(section => {
            section.style.display = 'block';
        });
        return;
    }
    
    let hasVisibleCommands = false;
    
    // Filter command cards
    commandCards.forEach(card => {
        const commandText = card.textContent.toLowerCase();
        
        if (commandText.includes(filter)) {
            card.style.display = 'block';
            card.style.animation = 'fadeIn 0.3s ease';
            hasVisibleCommands = true;
            
            // Highlight the search term
            const codeElements = card.querySelectorAll('code');
            codeElements.forEach(code => {
                const originalText = code.textContent;
                if (originalText.toLowerCase().includes(filter)) {
                    const regex = new RegExp(`(${filter})`, 'gi');
                    code.innerHTML = originalText.replace(regex, '<mark>$1</mark>');
                }
            });
        } else {
            card.style.display = 'none';
        }
    });
    
    // Show/hide sections based on whether they have visible commands
    sections.forEach(section => {
        const visibleCards = section.querySelectorAll('.command-card:not([style*="display: none"])');
        const sectionText = section.textContent.toLowerCase();
        
        if (visibleCards.length > 0 || sectionText.includes(filter)) {
            section.style.display = 'block';
        } else {
            section.style.display = 'none';
        }
    });
    
    // Show "no results" message if needed
    if (!hasVisibleCommands) {
        showNoResults(filter);
    } else {
        hideNoResults();
    }
}

// Show no results message
function showNoResults(searchTerm) {
    let noResultsDiv = document.getElementById('no-results');
    
    if (!noResultsDiv) {
        noResultsDiv = document.createElement('div');
        noResultsDiv.id = 'no-results';
        noResultsDiv.className = 'info-box';
        noResultsDiv.style.textAlign = 'center';
        noResultsDiv.style.padding = '40px';
        
        document.querySelector('.content').insertBefore(
            noResultsDiv,
            document.querySelector('.doc-section')
        );
    }
    
    noResultsDiv.innerHTML = `
        <h3>😕 No results found for "${searchTerm}"</h3>
        <p>Try searching for:</p>
        <ul style="list-style: none; padding: 0;">
            <li>Command names (e.g., "generate-poco")</li>
            <li>Command groups (e.g., "profile", "class")</li>
            <li>Technologies (e.g., "webapi", "grpc", "blazor")</li>
            <li>Operations (e.g., "sync", "mapping", "import")</li>
        </ul>
    `;
    noResultsDiv.style.display = 'block';
}

// Hide no results message
function hideNoResults() {
    const noResultsDiv = document.getElementById('no-results');
    if (noResultsDiv) {
        noResultsDiv.style.display = 'none';
    }
}

// Clear highlights when search is cleared
document.getElementById('searchInput').addEventListener('input', function() {
    if (this.value === '') {
        // Remove all highlights
        document.querySelectorAll('mark').forEach(mark => {
            mark.outerHTML = mark.textContent;
        });
    }
});

// Add fade-in animation for search results
const style = document.createElement('style');
style.innerHTML = `
    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(10px); }
        to { opacity: 1; transform: translateY(0); }
    }
    
    mark {
        background: #fbbf24;
        color: #1e293b;
        padding: 2px 4px;
        border-radius: 3px;
        font-weight: 600;
    }
`;
document.head.appendChild(style);

// Mobile menu toggle (for responsive design)
function createMobileMenuToggle() {
    if (window.innerWidth <= 768) {
        if (!document.querySelector('.menu-toggle')) {
            const toggle = document.createElement('button');
            toggle.className = 'menu-toggle';
            toggle.innerHTML = '☰';
            toggle.style.cssText = `
                position: fixed;
                top: 20px;
                left: 20px;
                z-index: 1001;
                background: var(--primary-color);
                color: white;
                border: none;
                padding: 10px 15px;
                border-radius: 8px;
                font-size: 1.5rem;
                cursor: pointer;
                box-shadow: 0 4px 15px rgba(37, 99, 235, 0.3);
            `;
            
            toggle.addEventListener('click', () => {
                document.querySelector('.sidebar').classList.toggle('open');
            });
            
            document.body.appendChild(toggle);
        }
    }
}

// Initialize
window.addEventListener('load', () => {
    createMobileMenuToggle();
    
    // Add click outside to close mobile menu
    document.addEventListener('click', (e) => {
        const sidebar = document.querySelector('.sidebar');
        const toggle = document.querySelector('.menu-toggle');
        
        if (window.innerWidth <= 768) {
            if (!sidebar.contains(e.target) && !toggle.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        }
    });
});

// Update mobile menu on resize
window.addEventListener('resize', () => {
    createMobileMenuToggle();
});

// Add copy button to code blocks
document.querySelectorAll('pre code').forEach(codeBlock => {
    const pre = codeBlock.parentElement;
    const button = document.createElement('button');
    button.className = 'copy-button';
    button.innerHTML = '📋 Copy';
    button.style.cssText = `
        position: absolute;
        top: 10px;
        right: 10px;
        background: var(--primary-color);
        color: white;
        border: none;
        padding: 5px 12px;
        border-radius: 6px;
        cursor: pointer;
        font-size: 0.8rem;
        opacity: 0;
        transition: opacity 0.3s ease;
    `;
    
    pre.style.position = 'relative';
    
    pre.addEventListener('mouseenter', () => {
        button.style.opacity = '1';
    });
    
    pre.addEventListener('mouseleave', () => {
        button.style.opacity = '0';
    });
    
    button.addEventListener('click', () => {
        const code = codeBlock.textContent;
        navigator.clipboard.writeText(code).then(() => {
            button.innerHTML = '✅ Copied!';
            setTimeout(() => {
                button.innerHTML = '📋 Copy';
            }, 2000);
        });
    });
    
    pre.appendChild(button);
});

// Add keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Ctrl/Cmd + K to focus search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        document.getElementById('searchInput').focus();
    }
    
    // Escape to clear search
    if (e.key === 'Escape') {
        const searchInput = document.getElementById('searchInput');
        searchInput.value = '';
        searchInput.blur();
        searchCommands();
    }
});

// Add tooltip to indicate keyboard shortcut
const searchInput = document.getElementById('searchInput');
searchInput.setAttribute('placeholder', 'Search commands... (Ctrl+K)');

console.log('🐝 BeepDM CLI Documentation loaded successfully!');
console.log('📖 Total Commands: 78');
console.log('✨ Use Ctrl+K to focus search');

