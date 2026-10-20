"""Create labeled planning sketches. These values never enter the event database."""
from html import escape
from pathlib import Path


def draw(filename, title, rows, suffix, maximum):
    height = 150 + len(rows) * 55
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="760" height="{height}" viewBox="0 0 760 {height}" role="img" aria-label="{escape(title)} planning mockup">',
             f'<rect width="760" height="{height}" rx="12" fill="#172532"/>',
             '<text x="30" y="32" fill="#edca87" font-family="sans-serif" font-size="13">PLANNING MOCKUP · ILLUSTRATIVE VALUES · NOT PLAYTEST RESULTS</text>',
             f'<text x="30" y="70" fill="#e3edf1" font-family="sans-serif" font-size="25">{escape(title)}</text>']
    for index, (label, value, color) in enumerate(rows):
        y = 100 + index * 55
        parts += [f'<text x="30" y="{y+20}" fill="#e3edf1" font-family="sans-serif" font-size="15">{escape(label)}</text>',
                  f'<rect x="250" y="{y}" width="400" height="28" rx="3" fill="#31434f"/>',
                  f'<rect x="250" y="{y}" width="{400*value/maximum}" height="28" rx="3" fill="{color}"/>',
                  f'<text x="668" y="{y+20}" fill="#e3edf1" font-family="sans-serif" font-size="15">{value}{suffix}</text>']
    parts.append('</svg>')
    filename.write_text('\n'.join(parts), encoding='utf-8')


if __name__ == '__main__':
    root = Path(__file__).parent / 'dashboard' / 'mockups'
    root.mkdir(parents=True, exist_ok=True)
    draw(root/'01-completion.svg', 'Completion rate by level', [('Tutorial',80,'#a8ead0'),('Level 1',55,'#a8ead0')], '%',100)
    draw(root/'02-duration.svg', 'Median active completion time', [('Tutorial',45,'#80bde9'),('Level 1',90,'#80bde9')], 's',100)
    draw(root/'03-checkpoints.svg', 'Checkpoint reach rate within a level', [('A / Introduction',100,'#80bde9'),('B / Light puzzle',75,'#80bde9'),('C / Exit approach',45,'#80bde9')], '%',100)
    draw(root/'04-failure.svg', 'Death rate with restart context', [('Tutorial / died',15,'#f29593'),('Tutorial / restarted',5,'#edca87'),('Level 1 / died',35,'#f29593'),('Level 1 / restarted',10,'#edca87')], '%',100)
    print(root.resolve())
