import sys
import csv
import json
import datetime
from pathlib import Path
from collections import defaultdict

markdeep_head = """
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/apidoc.css?"">
<script src="https://morgan3d.github.io/markdeep/latest/markdeep.min.js?" charset="utf-8"></script>
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
    markdown.write('Name|Count|Type|Size|WastedSize|FileName|Container\n')
    markdown.write('----|-----|----|----|----------|--------|---------\n')
    # markdown.write('# %s\n' % (self.getUniqueName()))
    with open(filename, encoding='utf-8') as infile:
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
            asset_item['items'].append(row)
            asset_item['wasted'] = int(row['Size']) * (len(asset_item['items']) - 1) 
            # print(row['Name'], file_size)

    total_wasted = 0
    for k, v in assets.items():
        total_wasted += v['wasted']        
    markdown.write('Total||||%s||\n' % ('**%s**' % pretty_number(total_wasted)))

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
            size = int(row['Size'])
            if 'png' in asset_filename:
                asset_filename = '![](%s border="2" width="50%%")' % asset_filename
            markdown.write('%s|%d|%s|%s|%s|%s|%s\n' % (
                row['Name'],
                len(items),
                row['Type'],
                pretty_number(size),
                '**%s**' % pretty_number(v['wasted']),
                asset_filename,
                ',<br>'.join(containers),
            ))

    print('\nreport -> %s\n' % (dir_name / 'pkg.html'))

if __name__ == '__main__':
    pkg_csv = 'd:/svn_pool/pkg-doctor/hff_1a52b8498d42e.1620807276/viz/pkg.csv'
    if len(sys.argv) > 1:
        pkg_csv = sys.argv[1]    
    process_pkg_csv(pkg_csv)