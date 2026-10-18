import importlib.util
from pathlib import Path
import tempfile
import unittest

spec=importlib.util.spec_from_file_location('art_contract',Path(__file__).parents[1]/'Tools/Pipeline/art_contract.py')
art=importlib.util.module_from_spec(spec); spec.loader.exec_module(art)

class ArtContractTests(unittest.TestCase):
    def test_unowned_partial_unchanged_edit_and_recipe(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder).resolve()
            self.assertFalse(art.preflight(path,'r1'))
            (path/'mesh.bin').write_bytes(b'fixture only')
            with self.assertRaises(ValueError): art.preflight(path,'r1')
            art.finish(path,'r1',{'fixture':True})
            self.assertTrue(art.preflight(path,'r1'))
            with self.assertRaises(ValueError): art.preflight(path,'r2')
            with self.assertRaises(FileExistsError): art.finish(path,'r1',{})
            (path/'mesh.bin').write_bytes(b'owner edit')
            with self.assertRaises(ValueError): art.preflight(path,'r1')

    def test_symlink_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder).resolve(); (root/'real').mkdir(); (root/'link').symlink_to(root/'real',target_is_directory=True)
            with self.assertRaises(ValueError): art.preflight(root/'link','r1')

if __name__=='__main__': unittest.main()
