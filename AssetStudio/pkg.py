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
<style>
.md h1 {
    color: #ff6600;  
}
.md div.title {
    background-color: #ff6600;  
}
</style>

"""
def pretty_number(num):
    if num < 1e3:
        return str(num)
    if num < 1e6:
        return "%.1fK" % (num/1e3)
    if num < 1e9:
        return "%.1fM" % (num/1e6)
    if num < 1e12:
        return "%.1fG" % (num/1e9)
    return str(num)

def process_pkg_csv(filename):
    assets = {}
    dir_name = Path(filename).parent
    # print(filename, dir_name)

    markdown = open(dir_name / 'pkg.html', 'w', encoding='utf-8')
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
            hash = row['Hash']
            filename = row['FileName']
            if filename:
                file_path = dir_name / filename
                if file_path.exists():
                    file_size = file_path.stat().st_size
                    hash = row['Type'] + str(file_size)
            if hash not in assets:
                assets[hash] = {
                    'wasted': 0,
                    'items': []
                }
            asset_item = assets[hash]
            row['Size'] = int(row['Size'])
            asset_item['items'].append(row)
            asset_item['wasted'] = row['Size'] * (len(asset_item['items']) - 1) 
            # print(row['Name'], md5)

    total_bytes = 0
    total_texture_bytes = 0
    total_shader_bytes = 0
    total_font_bytes = 0
    total_mesh_bytes = 0
    total_audio_bytes = 0
    total_text_bytes = 0
    total_animation_bytes = 0
    total_wasted_bytes = 0
    total_uncompressed_bytes = 0
    total_uncompressed_count = 0
    for k, v in assets.items():
        total_wasted_bytes += v['wasted']
        items = v['items']
        row = items[0]
        items_bytes = row['Size'] * len(items)
        total_bytes += items_bytes
        if row['Type'] == 'Texture2D':
            total_texture_bytes += items_bytes
        elif row['Type'] == 'Shader':
            total_shader_bytes += items_bytes
        elif row['Type'] == 'Font':
            total_font_bytes += items_bytes
        elif row['Type'] == 'Mesh':
            total_mesh_bytes += items_bytes
        elif row['Type'] == 'AudioClip':
            total_audio_bytes += items_bytes
        elif row['Type'] == 'AnimationClip':
            total_animation_bytes += items_bytes
        elif row['Type'] == 'TextAsset':
            total_text_bytes += items_bytes

        if row['Type'] == 'Texture2D' and 'BC' not in row['Format'] and 'TC' not in row['Format']:
            total_uncompressed_bytes += items_bytes
            total_uncompressed_count += len(items)

    markdown.write('# 包体概览\n')
    markdown.write('- 资产尺寸: **%s**\n' % pretty_number(total_bytes))
    markdown.write('  - Texture: **%s** (%.2f%%)\n' % (pretty_number(total_texture_bytes), total_texture_bytes * 100 / total_bytes))
    markdown.write('  - Mesh: **%s** (%.2f%%)\n' % (pretty_number(total_mesh_bytes), total_mesh_bytes * 100 / total_bytes))
    markdown.write('  - AnimationClip: **%s** (%.2f%%)\n' % (pretty_number(total_animation_bytes), total_animation_bytes * 100 / total_bytes))
    markdown.write('  - TextAsset: **%s** (%.2f%%)\n' % (pretty_number(total_text_bytes), total_text_bytes * 100 / total_bytes))
    markdown.write('  - Shader: **%s** (%.2f%%)\n' % (pretty_number(total_shader_bytes), total_shader_bytes * 100 / total_bytes))
    markdown.write('  - Font: **%s** (%.2f%%)\n' % (pretty_number(total_font_bytes), total_font_bytes * 100 / total_bytes))
    markdown.write('  - AudioClip: **%s** (%.2f%%)\n' % (pretty_number(total_audio_bytes), total_audio_bytes * 100 / total_bytes))
    markdown.write('- 重复入包尺寸: **%s**\n' % pretty_number(total_wasted_bytes))
    markdown.write('- 未压缩贴图尺寸: **%s**\n' % pretty_number(total_uncompressed_bytes))
    markdown.write('\n')

    markdown.write('# 重复入包\n')
    markdown.write('Name|Type|Size|Wasted|Dimension|Format|Preview|Container\n')
    markdown.write('----|----|----|------|---------|------|-------|---------\n')

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
            if 'png' in asset_filename:
                preview = '![](%s border="2")' % asset_filename
            else:
                preview = '' # save web page space
            type = row['Type']
            if type == 'Texture2D':
                type = 'Texture'
            markdown.write('%s|%s|%s|%s|%s|%s|%s|%s\n' % (
                row['Name'],
                type,
                '%s*%d' % (pretty_number(row['Size']), len(items)),
                '**%s**' % pretty_number(v['wasted']),
                row['Dimension'],
                row['Format'],
                preview,
                ', '.join(containers),
            ))
    markdown.write('\n')

    markdown.write('# 未压缩贴图\n')
    markdown.write('Name|Size|Dimension|Format|Preview|Container\n')
    markdown.write('----|----|---------|------|-------|---------\n')
    for k in dict(sorted(assets.items(), key=lambda item: item[1]['items'][0]['Size'], reverse=True)):
        v = assets[k]    
        items = v['items']
        row = items[0]
        if row['Type'] != 'Texture2D':
            continue
        format = row['Format']
        if 'BC' in format or 'TC' in format or 'Alpha8' in format:
            continue
        asset_name = row['Name']
        asset_filename = row['FileName']
        if 'xdsdk' in asset_name or 'taptap' in asset_name or 'UnityWatermark' in asset_name:
            # from SDK
            continue
        
        if 'png' in asset_filename:
            preview = '![](%s border="2")' % asset_filename
        else:
            preview = ''   
        markdown.write('%s|%s|%s|%s|%s|%s\n' % (
            row['Name'],
            '%s*%d' % (pretty_number(row['Size']), len(items)),
            row['Dimension'],
            row['Format'],
            preview,
            row['Container'],
        ))
    markdown.write('\n')

    markdown.write('# 大尺寸贴图\n')
    markdown.write('Name|Size|Dimension|Format|Preview|Container\n')
    markdown.write('----|----|---------|------|-------|---------\n')
    for k in dict(sorted(assets.items(), key=lambda item: item[1]['items'][0]['Size'], reverse=True)):
        v = assets[k]    
        items = v['items']
        row = items[0]
        if row['Type'] != 'Texture2D':
            continue
        dimension = row['Dimension']
        if '1024' in dimension or '2048' in dimension or '4096' in dimension:
            asset_name = row['Name']
            asset_filename = row['FileName']
            if '_1024' in asset_name or '_2048' in asset_name or 'sactx' in asset_name or 'lightmap' in asset_name.lower():
                continue
            if 'png' in asset_filename:
                preview = '![](%s border="2")' % asset_filename
            else:
                preview = ''   
            markdown.write('%s|%s|%s|%s|%s|%s\n' % (
                row['Name'],
                pretty_number(row['Size']),
                row['Dimension'],
                row['Format'],
                preview,
                row['Container'],
            ))            
    markdown.write('\n')

    print('\nreport -> %s\n' % (dir_name / 'pkg.html'))

if __name__ == '__main__':
    pkg_csv = 'c:/svn_pool/pkg-doctor/_fp0521/base-pkg/pkg.csv'
    if len(sys.argv) > 1:
        pkg_csv = sys.argv[1]    
    process_pkg_csv(pkg_csv)