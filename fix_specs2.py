import os
import re

specs_dir = r'c:\Users\maxsa\Documents\24_7_Agent\VISTA_Project\Specs'

def ireplace(pattern, repl, text):
    return re.sub(pattern, repl, text, flags=re.IGNORECASE)

for root, dirs, files in os.walk(specs_dir):
    for file in files:
        if file.endswith('.md'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            new_content = content
            # Replace ViewModel(s) inside strings like MainWindowViewModel -> MainWindowPresenter
            new_content = re.sub(r'ViewModels', 'Presenters', new_content)
            new_content = re.sub(r'ViewModel', 'Presenter', new_content)
            new_content = re.sub(r'viewmodels', 'presenters', new_content)
            new_content = re.sub(r'viewmodel', 'presenter', new_content)
            
            # Wpf -> WinForms
            new_content = re.sub(r'Wpf', 'WinForms', new_content)
            new_content = re.sub(r'WPF', 'WinForms', new_content)
            
            # MVVM -> MVP
            new_content = re.sub(r'MVVM', 'MVP', new_content)
            new_content = re.sub(r'mvvm', 'mvp', new_content)
            
            if new_content != content:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Updated {path}")
