import os
import re

specs_dir = r'c:\Users\maxsa\Documents\24_7_Agent\VISTA_Project\Specs'

replacements = [
    (r'WPF_Applications', r'WinForms_Applications'),
    (r'\bWPF\b', r'WinForms'),
    (r'\bMVVM\b', r'MVP'),
    (r'\bViewModels?\b', lambda m: 'Presenters' if m.group(0).endswith('s') else 'Presenter'),
    (r'\bViewModel\b', r'Presenter'),
    (r'\bViewModels\b', r'Presenters'),
    (r'\bXAML\b', r'Designer code'),
    (r'\bxaml\b', r'Designer code'),
    (r'\.xaml\b', r'.Designer.vb')
]

for root, dirs, files in os.walk(specs_dir):
    for file in files:
        if file.endswith('.md'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            new_content = content
            for pattern, repl in replacements:
                new_content = re.sub(pattern, repl, new_content)
                
            if new_content != content:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Updated {path}")
