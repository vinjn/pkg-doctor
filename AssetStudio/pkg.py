import sys
import csv
import json
import datetime
from pathlib import Path
from collections import defaultdict

markdeep_head = """
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/newsmag.css?"">
<script>markdeepOptions={tocStyle:'medium'};</script>
<script src="https://casual-effects.com/markdeep/markdeep.min.js?" charset="utf-8"></script>
"""
def pretty_number(num):
    if num < 1e3:
        return str(num)
    if num < 1e6:
        return "%.1fK" % (num/1e3)
    if num < 1e9:
        return "%.1fM" % (num/1e6)
    return str(num)

def process_pkg_csv(filename):
    assets = {}
    dir_name = Path(filename).parent
    # print(filename, dir_name)

    markdown = open(dir_name / 'pkg.html', 'w')
    markdown.write(markdeep_head)
    # markdown.write('# %s\n' % (self.getUniqueName()))
    with open(filename, encoding='utf-8') as infile:
        markdown.write('**pkg_doctor**\n')
        markdown.write(' %s\n' % filename)
        if 'tsv' in filename:
            reader = csv.DictReader(infile, delimiter='\t')
        else:
            reader = csv.DictReader(infile)
        for row in reader:
            # print(row)
            file_path = dir_name / row['FileName']
            file_size = file_path.stat().st_size
            if file_size not in assets:
                assets[file_size] = {
                    'wasted': 0,
                    'items': []
                }
            asset_item = assets[file_size]
            row['Size'] = int(row['Size'])
            asset_item['items'].append(row)
            asset_item['wasted'] = row['Size'] * (len(asset_item['items']) - 1) 
            # print(row['Name'], file_size)

    markdown.write('# Duplicated Assets\n')
    markdown.write('Name|#|Size|Wasted|Dimension|Format|Preview|Container\n')
    markdown.write('----|-|----|------|---------|------|-------|---------\n')
    total_wasted = 0
    for k, v in assets.items():
        total_wasted += v['wasted']        
    markdown.write('Total||||%s||||\n' % ('**%s**' % pretty_number(total_wasted)))

    for k in dict(sorted(assets.items(), key=lambda item: item[1]['wasted'], reverse=True)):
        v = assets[k]
        items = v['items']
        if len(items) > 1:
            # duplicated assets
            row = items[0]
            containers = []
            for item in items:
                containers.append(item['Container'])
            asset_filename = row['FileName']
            size = row['Size']
            if 'png' in asset_filename:
                preview = '![](%s border="2")' % asset_filename
            else:
                preview = '' # save web page space
            markdown.write('%s|%s|%s|%s|%s|%s|%s|%s\n' % (
                row['Name'],
                "%d%s" % (len(items), row['Type'][0]),
                pretty_number(size),
                '**%s**' % pretty_number(v['wasted']),
                row['Dimension'],
                row['Format'],
                preview,
                ',<br>'.join(containers),
            ))

    markdown.write('# Uncompressed Textures\n')
    markdown.write('Name|#|Size|Dimension|Format|Preview|Container\n')
    markdown.write('----|-|----|---------|------|-------|---------\n')
    for k in dict(sorted(assets.items(), key=lambda item: item[1]['items'][0]['Size'], reverse=True)):
        v = assets[k]    
        items = v['items']
        row = items[0]
        if row['Type'] != 'Texture2D':
            continue
        format = row['Format']
        if 'BC' in format or 'TC' in format:
            continue
        preview = '![](%s border="2" width="50%%")' % row['FileName']
        markdown.write('%s|%d|%s|%s|%s|%s|%s\n' % (
            row['Name'],
            len(items),
            pretty_number(row['Size']),
            row['Dimension'],
            row['Format'],
            preview,
            row['Container'],
        ))
    markdown.write('\n')

    print('\nreport -> %s\n' % (dir_name / 'pkg.html'))

if __name__ == '__main__':
    pkg_csv = 'd:/t3-202105120931fc9190.1620783875-pkg/pkg.csv'
    if len(sys.argv) > 1:
        pkg_csv = sys.argv[1]    
    process_pkg_csv(pkg_csv)